using Psel.Modules.Assessment.Contracts;
using Psel.Modules.Assessment.Domain;
using Psel.Modules.Execution;
using AssessmentEntity = Psel.Modules.Assessment.Domain.Assessment;

namespace Psel.Modules.Assessment.Services;

public sealed record AssessmentGrade(
    AssessmentEntity Assessment,
    string SanitizedStandardOutput);

/// <summary>
/// Deterministic scoring rules:
/// - A non-successful execution scores zero and marks every test NotRun.
/// - A successful execution with an unreadable harness result marks every test
///   NotRun and returns InternalRunnerError.
/// - Otherwise every selected test is weighted equally. The score is
///   passed * 100 / total using integer arithmetic.
/// - All tests passing is Accepted. Any per-test harness error is RuntimeError.
///   Every other successful execution is WrongAnswer.
/// </summary>
internal static class AssessmentGrader
{
    public static AssessmentGrade Grade(AssessmentInput input)
    {
        var sanitized = AssessmentProtocol.StripResults(input.StandardOutput);
        var now = DateTimeOffset.UtcNow;

        if (input.ExecutionStatus != ExecutionStatus.Succeeded)
        {
            return CreateGrade(
                input,
                MapOutcome(input.ExecutionStatus),
                sanitized,
                statuses: null,
                now);
        }

        if (input.Tests.Count == 0)
        {
            return CreateGrade(
                input,
                AssessmentOutcome.InternalRunnerError,
                sanitized,
                statuses: null,
                now);
        }

        var observations = AssessmentProtocol.TryRead(input.StandardOutput);

        if (observations is null || !IsComplete(observations, input.Tests))
        {
            return CreateGrade(
                input,
                AssessmentOutcome.InternalRunnerError,
                sanitized,
                statuses: null,
                now);
        }

        var byId = observations.ToDictionary(
            observation => observation.Id!,
            StringComparer.Ordinal);
        var statuses = new List<AssessmentTestStatus>(input.Tests.Count);
        var anyErrored = false;

        foreach (var test in input.Tests)
        {
            var observation = byId[test.Id];

            if (observation.Errored)
            {
                anyErrored = true;
                statuses.Add(AssessmentTestStatus.Failed);
            }
            else
            {
                statuses.Add(
                    observation.Passed
                        ? AssessmentTestStatus.Passed
                        : AssessmentTestStatus.Failed);
            }
        }

        var passed = statuses.Count(status => status == AssessmentTestStatus.Passed);
        var outcome = anyErrored
            ? AssessmentOutcome.RuntimeError
            : passed == input.Tests.Count
                ? AssessmentOutcome.Accepted
                : AssessmentOutcome.WrongAnswer;

        return CreateGrade(input, outcome, sanitized, statuses, now, passed);
    }

    private static AssessmentGrade CreateGrade(
        AssessmentInput input,
        AssessmentOutcome outcome,
        string sanitized,
        IReadOnlyList<AssessmentTestStatus>? statuses,
        DateTimeOffset now,
        int? passedOverride = null)
    {
        var publicTests = input.Tests.Count(test => test.IsPublic);
        var hiddenTests = input.Tests.Count - publicTests;
        var passed = passedOverride
            ?? statuses?.Count(status => status == AssessmentTestStatus.Passed)
            ?? 0;
        var total = input.Tests.Count;
        var score = total == 0 || outcome is not (
            AssessmentOutcome.Accepted
            or AssessmentOutcome.WrongAnswer
            or AssessmentOutcome.RuntimeError)
            ? 0
            : passed * 100 / total;

        var assessment = AssessmentEntity.Create(
            input.SubmissionId,
            input.ChallengeSlug,
            input.LanguageId,
            input.Purpose,
            outcome,
            score,
            passed,
            total,
            CountPassed(input.Tests, statuses, isPublic: true),
            publicTests,
            CountPassed(input.Tests, statuses, isPublic: false),
            hiddenTests,
            now);

        for (var index = 0; index < input.Tests.Count; index++)
        {
            var test = input.Tests[index];
            var status = statuses?[index] ?? AssessmentTestStatus.NotRun;

            assessment.AddTestResult(AssessmentTestResult.Create(
                index,
                test.Id,
                test.Name,
                test.Description,
                test.IsPublic ? AssessmentVisibility.Public : AssessmentVisibility.Hidden,
                status));
        }

        return new AssessmentGrade(assessment, sanitized);
    }

    private static int CountPassed(
        IReadOnlyList<AssessmentTestDefinition> tests,
        IReadOnlyList<AssessmentTestStatus>? statuses,
        bool isPublic)
    {
        if (statuses is null)
        {
            return 0;
        }

        var count = 0;

        for (var index = 0; index < tests.Count; index++)
        {
            if (tests[index].IsPublic == isPublic
                && statuses[index] == AssessmentTestStatus.Passed)
            {
                count++;
            }
        }

        return count;
    }

    private static bool IsComplete(
        IReadOnlyList<ObservedTest> observations,
        IReadOnlyList<AssessmentTestDefinition> tests)
    {
        if (observations.Count != tests.Count)
        {
            return false;
        }

        var ids = new HashSet<string>(StringComparer.Ordinal);

        foreach (var observation in observations)
        {
            if (string.IsNullOrWhiteSpace(observation.Id) || !ids.Add(observation.Id))
            {
                return false;
            }
        }

        return tests.All(test => ids.Contains(test.Id));
    }

    private static AssessmentOutcome MapOutcome(ExecutionStatus status)
    {
        return status switch
        {
            ExecutionStatus.CompilationError => AssessmentOutcome.CompilationError,
            ExecutionStatus.RuntimeError => AssessmentOutcome.RuntimeError,
            ExecutionStatus.TimeLimitExceeded => AssessmentOutcome.TimeLimitExceeded,
            ExecutionStatus.MemoryLimitExceeded => AssessmentOutcome.MemoryLimitExceeded,
            ExecutionStatus.OutputLimitExceeded => AssessmentOutcome.OutputLimitExceeded,
            _ => AssessmentOutcome.InternalRunnerError
        };
    }
}
