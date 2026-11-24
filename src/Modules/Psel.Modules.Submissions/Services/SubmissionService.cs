using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Psel.Infrastructure.Persistence;
using Psel.Modules.Challenges.Packages;
using Psel.Modules.Execution;
using Psel.Modules.Progress.Contracts;
using Psel.Modules.Submissions.Contracts;
using Psel.Modules.Submissions.Domain;

namespace Psel.Modules.Submissions.Services;

internal sealed class SubmissionValidationException(string message) : Exception(message);

internal sealed class SubmissionQueueUnavailableException(string message) : Exception(message);

/// <summary>
/// Validates a submission against trusted challenge packages, persists it, and
/// hands it to the execution worker. Client input never selects an image,
/// command, mount, or harness.
/// </summary>
internal sealed class SubmissionService(
    PselDbContext dbContext,
    IChallengeCatalog challengeCatalog,
    IChallengeExecutionSource executionSource,
    IEnumerable<ILanguageRunner> runners,
    SubmissionQueue queue,
    IExaminationPolicy examinationPolicy)
{
    private const int MaxFileBytes = 128 * 1024;
    private const int MaxTotalBytes = 256 * 1024;

    private static readonly JsonSerializerOptions SourceFileJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<SubmissionCreated> CreateAsync(
        CreateSubmissionRequest request,
        CancellationToken cancellationToken)
    {
        var challengeSlug = request.ChallengeSlug?.Trim();
        var challenge = string.IsNullOrWhiteSpace(challengeSlug)
            ? null
            : challengeCatalog.FindBySlug(challengeSlug);

        if (challenge is null)
        {
            throw new SubmissionValidationException("The challenge does not exist.");
        }

        var languageId = request.LanguageId?.Trim();
        var language = challenge.Languages.FirstOrDefault(candidate =>
            string.Equals(candidate.Id, languageId, StringComparison.OrdinalIgnoreCase));

        if (language is null)
        {
            throw new SubmissionValidationException(
                "The selected language is not part of this challenge.");
        }

        if (!language.CanExecute
            || !executionSource.CanExecute(challenge.Slug, language.Id))
        {
            throw new SubmissionValidationException(
                "This challenge cannot be executed with the selected language.");
        }

        var runner = runners.FirstOrDefault(candidate =>
            string.Equals(candidate.LanguageId, language.Id, StringComparison.OrdinalIgnoreCase));

        if (runner is null)
        {
            throw new SubmissionValidationException(
                "The server is not configured to execute the selected language.");
        }

        var files = ValidateFiles(request.Files, runner);
        var purpose = ParsePurpose(request.Purpose);

        if (request.ExaminationId is { } examinationId)
        {
            if (purpose != SubmissionPurpose.Submit)
            {
                throw new SubmissionValidationException(
                    "Examination submissions must use the Submit purpose.");
            }

            var authorization = await examinationPolicy.AuthorizeAttemptAsync(
                examinationId,
                challenge.Slug,
                cancellationToken);

            if (!authorization.IsAllowed)
            {
                throw new SubmissionValidationException(
                    authorization.RejectionReason ?? "The examination is not active.");
            }
        }

        var sourceFilesJson = JsonSerializer.Serialize(files);
        var now = DateTimeOffset.UtcNow;
        var submission = Submission.Create(
            challenge.Slug,
            language.Id,
            purpose,
            sourceFilesJson,
            now);

        dbContext.Set<Submission>().Add(submission);

        if (request.ExaminationId is { } attemptExaminationId)
        {
            await examinationPolicy.RecordAttemptAsync(
                attemptExaminationId,
                submission.Id,
                cancellationToken);
        }
        else
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        if (!queue.TryEnqueue(submission.Id))
        {
            submission.MarkUnavailable(
                "The execution queue is full. Try again shortly.",
                DateTimeOffset.UtcNow);
            await dbContext.SaveChangesAsync(cancellationToken);

            throw new SubmissionQueueUnavailableException(
                "The execution queue is full. Try again shortly.");
        }

        return new SubmissionCreated(
            submission.Id,
            submission.Status.ToString(),
            submission.CreatedAtUtc);
    }

    public async Task<SubmissionDetail?> FindAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var submission = await dbContext
            .Set<Submission>()
            .AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);

        return submission is null ? null : ToDetail(submission);
    }

    public async Task<LatestSubmission?> FindLatestAsync(
        string challengeSlug,
        string languageId,
        CancellationToken cancellationToken)
    {
        var submissions = await dbContext
            .Set<Submission>()
            .AsNoTracking()
            .Where(candidate =>
                candidate.ChallengeSlug == challengeSlug
                && candidate.LanguageId == languageId)
            .ToListAsync(cancellationToken);

        // SQLite cannot translate DateTimeOffset ordering, so order on the client.
        var submission = submissions
            .OrderByDescending(candidate => candidate.CreatedAtUtc)
            .ThenByDescending(candidate => candidate.Id)
            .FirstOrDefault();

        return submission is null
            ? null
            : new LatestSubmission(ToDetail(submission), ReadSourceFiles(submission));
    }

    private static IReadOnlyList<SubmissionSourceFile> ReadSourceFiles(Submission submission)
    {
        try
        {
            var files = JsonSerializer.Deserialize<IReadOnlyList<ExecutionFile>>(
                submission.SourceFilesJson,
                SourceFileJsonOptions);

            return files is null
                ? []
                : files
                    .Select(file => new SubmissionSourceFile(file.Path, file.Content))
                    .ToArray();
        }
        catch (JsonException)
        {
            return [];
        }
    }

    public static SubmissionDetail ToDetail(Submission submission)
    {
        return new SubmissionDetail(
            submission.Id,
            submission.ChallengeSlug,
            submission.LanguageId,
            submission.Status.ToString(),
            submission.IsTerminal,
            submission.CreatedAtUtc,
            submission.StartedAtUtc,
            submission.CompletedAtUtc,
            submission.DurationMs,
            submission.ExitCode,
            submission.StandardOutput,
            submission.StandardError,
            submission.FailureMessage,
            submission.OutputTruncated,
            submission.Purpose.ToString());
    }

    private static SubmissionPurpose ParsePurpose(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return SubmissionPurpose.Run;
        }

        return Enum.TryParse<SubmissionPurpose>(
            value.Trim(),
            ignoreCase: true,
            out var purpose)
            && Enum.IsDefined(purpose)
            ? purpose
            : throw new SubmissionValidationException(
                "Submission purpose must be Run or Submit.");
    }

    private static IReadOnlyList<ExecutionFile> ValidateFiles(
        IReadOnlyList<SubmissionFileInput>? inputs,
        ILanguageRunner runner)
    {
        if (inputs is null || inputs.Count == 0)
        {
            throw new SubmissionValidationException("At least one source file is required.");
        }

        var maxFiles = Math.Clamp(runner.MaxSubmissionFiles, 1, 10);

        if (inputs.Count > maxFiles)
        {
            throw new SubmissionValidationException(
                $"A submission may contain at most {maxFiles} source files.");
        }

        var files = new List<ExecutionFile>(inputs.Count);
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var totalBytes = 0;

        foreach (var input in inputs)
        {
            if (!ExecutionFileRules.IsSafeRelativePath(input?.Path, out var path))
            {
                throw new SubmissionValidationException(
                    "Source file paths must be relative paths inside the submission workspace.");
            }

            if (!paths.Add(path))
            {
                throw new SubmissionValidationException(
                    $"Source file path '{path}' is duplicated.");
            }

            if (!runner.AcceptsFile(path))
            {
                throw new SubmissionValidationException(
                    $"Source file '{path}' has an unsupported extension for {runner.LanguageId}.");
            }

            if (input?.Content is null)
            {
                throw new SubmissionValidationException(
                    $"Source file '{path}' must contain text content.");
            }

            var bytes = Encoding.UTF8.GetByteCount(input.Content);

            if (bytes > MaxFileBytes)
            {
                throw new SubmissionValidationException(
                    $"Source file '{path}' exceeds the maximum size of {MaxFileBytes} bytes.");
            }

            totalBytes += bytes;

            if (totalBytes > MaxTotalBytes)
            {
                throw new SubmissionValidationException(
                    $"A submission may contain at most {MaxTotalBytes} bytes of source code.");
            }

            files.Add(new ExecutionFile(path, input.Content));
        }

        return files;
    }
}
