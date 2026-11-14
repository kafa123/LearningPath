using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Psel.Infrastructure.Persistence;
using Psel.Modules.Assessment;
using Psel.Modules.Assessment.Contracts;
using Psel.Modules.Assessment.Services;
using Psel.Modules.Challenges.Packages;
using Psel.Modules.Execution;
using Psel.Modules.Submissions.Domain;

namespace Psel.Modules.Submissions.Services;

/// <summary>
/// Pulls queued submissions and executes them through the configured language
/// runner. Each submission gets its own database scope and is marked running
/// before the runner is invoked, so a crash leaves an observable stale row.
/// </summary>
internal sealed class SubmissionWorker(
    SubmissionQueue queue,
    IServiceScopeFactory scopeFactory,
    IChallengeExecutionSource executionSource,
    IEnumerable<ILanguageRunner> runners,
    IOptions<ExecutionOptions> options,
    ILogger<SubmissionWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var concurrency = Math.Clamp(options.Value.MaxConcurrentExecutions, 1, 16);
        using var gate = new SemaphoreSlim(concurrency, concurrency);
        var running = new List<Task>();

        try
        {
            await foreach (var submissionId in queue.ReadAllAsync(stoppingToken))
            {
                await gate.WaitAsync(stoppingToken);
                running.RemoveAll(task => task.IsCompleted);
                running.Add(Task.Run(
                    async () =>
                    {
                        try
                        {
                            await ProcessAsync(submissionId, stoppingToken);
                        }
                        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                        {
                            // Shutdown is handled by the runner and persisted as an interruption.
                        }
                        catch (Exception exception)
                        {
                            logger.LogError(
                                exception,
                                "Submission {SubmissionId} could not be processed.",
                                submissionId);
                        }
                        finally
                        {
                            gate.Release();
                        }
                    },
                    CancellationToken.None));
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Expected during application shutdown.
        }
        finally
        {
            try
            {
                await Task.WhenAll(running);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "One or more submissions failed during shutdown.");
            }
        }
    }

    private async Task ProcessAsync(Guid submissionId, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PselDbContext>();
        var submission = await dbContext
            .Set<Submission>()
            .FirstOrDefaultAsync(candidate => candidate.Id == submissionId, CancellationToken.None);

        if (submission is null || submission.IsTerminal || submission.Status != SubmissionStatus.Queued)
        {
            return;
        }

        submission.MarkRunning(DateTimeOffset.UtcNow);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var tests = SelectTests(
            executionSource.GetTestPlan(submission.ChallengeSlug),
            submission.Purpose);
        var outcome = await ExecuteAsync(submission, tests, cancellationToken);
        var assessments = scope.ServiceProvider.GetRequiredService<AssessmentService>();
        AssessmentGrade? grade = null;

        try
        {
            grade = assessments.Record(new AssessmentInput(
                submission.Id,
                submission.ChallengeSlug,
                submission.LanguageId,
                submission.Purpose.ToString(),
                outcome.Status,
                outcome.StandardOutput,
                tests
                    .Select(test => new AssessmentTestDefinition(
                        test.Id,
                        string.Equals(test.Visibility, "public", StringComparison.OrdinalIgnoreCase),
                        test.Name,
                        test.Description))
                    .ToList()));
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Assessment failed for submission {SubmissionId}.",
                submission.Id);
        }

        if (grade is not null)
        {
            outcome = outcome with { StandardOutput = grade.SanitizedStandardOutput };
        }

        submission.Complete(outcome, DateTimeOffset.UtcNow);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        logger.LogInformation(
            "Submission {SubmissionId} finished with {Status} and assessment {Outcome}.",
            submission.Id,
            submission.Status,
            grade?.Assessment.Outcome.ToString() ?? "Unavailable");
    }

    private static IReadOnlyList<ChallengeExecutionTest> SelectTests(
        ChallengeTestPlan? plan,
        SubmissionPurpose purpose)
    {
        if (plan is null)
        {
            return [];
        }

        if (purpose == SubmissionPurpose.Submit)
        {
            return plan.Tests;
        }

        return plan.Tests
            .Where(test => string.Equals(
                test.Visibility,
                "public",
                StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private async Task<ExecutionOutcome> ExecuteAsync(
        Submission submission,
        IReadOnlyList<ChallengeExecutionTest> tests,
        CancellationToken cancellationToken)
    {
        var harness = executionSource.GetHarness(submission.ChallengeSlug, submission.LanguageId);
        var runner = runners.FirstOrDefault(candidate =>
            string.Equals(
                candidate.LanguageId,
                submission.LanguageId,
                StringComparison.OrdinalIgnoreCase));

        if (harness is null || runner is null || tests.Count == 0)
        {
            return new ExecutionOutcome(
                ExecutionStatus.ExecutionUnavailable,
                null,
                0,
                string.Empty,
                string.Empty,
                "Execution is not available for this challenge and language.",
                false);
        }

        List<ExecutionFile>? files;

        try
        {
            files = JsonSerializer.Deserialize<List<ExecutionFile>>(submission.SourceFilesJson);
        }
        catch (JsonException exception)
        {
            logger.LogError(
                exception,
                "Submission {SubmissionId} contains unreadable source files.",
                submission.Id);

            return new ExecutionOutcome(
                ExecutionStatus.InternalError,
                null,
                0,
                string.Empty,
                string.Empty,
                "The stored submission could not be read.",
                false);
        }

        if (files is null || files.Count == 0)
        {
            return new ExecutionOutcome(
                ExecutionStatus.InternalError,
                null,
                0,
                string.Empty,
                string.Empty,
                "The stored submission has no source files.",
                false);
        }

        try
        {
            return await runner.ExecuteAsync(
                new ExecutionRequest(
                    submission.Id,
                    submission.ChallengeSlug,
                    submission.LanguageId,
                    files,
                    harness,
                    tests
                        .Select(test => new ExecutionTest(
                            test.Id,
                            test.Visibility,
                            test.DataJson))
                        .ToList()),
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return new ExecutionOutcome(
                ExecutionStatus.Cancelled,
                null,
                0,
                string.Empty,
                string.Empty,
                "Execution was interrupted.",
                false);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Language runner failed for submission {SubmissionId}.",
                submission.Id);

            return new ExecutionOutcome(
                ExecutionStatus.InternalError,
                null,
                0,
                string.Empty,
                string.Empty,
                "The execution engine failed unexpectedly.",
                false);
        }
    }
}
