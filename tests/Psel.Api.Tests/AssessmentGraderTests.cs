using Psel.Modules.Assessment.Contracts;
using Psel.Modules.Assessment.Domain;
using Psel.Modules.Assessment.Services;
using Psel.Modules.Execution;

namespace Psel.Api.Tests;

public sealed class AssessmentGraderTests
{
    private static readonly Guid SubmissionId = Guid.Parse(
        "11111111-1111-1111-1111-111111111111");

    [Fact]
    public void Grade_AllTestsPass_ReturnsAcceptedWithFullScore()
    {
        var tests = new[]
        {
            Public("public-1"),
            Public("public-2"),
            Hidden("hidden-1"),
            Hidden("hidden-2")
        };

        var grade = Grade(
            ExecutionStatus.Succeeded,
            AssessmentTestOutput.BuildFromObservations(
                ("public-1", true, false),
                ("public-2", true, false),
                ("hidden-1", true, false),
                ("hidden-2", true, false)),
            tests);

        Assert.Equal(AssessmentOutcome.Accepted, grade.Assessment.Outcome);
        Assert.True(grade.Assessment.IsAccepted);
        Assert.Equal(100, grade.Assessment.Score);
        Assert.Equal(4, grade.Assessment.PassedTests);
        Assert.Equal(4, grade.Assessment.TotalTests);
        Assert.Equal(2, grade.Assessment.PublicPassed);
        Assert.Equal(2, grade.Assessment.PublicTotal);
        Assert.Equal(2, grade.Assessment.HiddenPassed);
        Assert.Equal(2, grade.Assessment.HiddenTotal);
        Assert.All(
            grade.Assessment.TestResults,
            result => Assert.Equal(AssessmentTestStatus.Passed, result.Status));
    }

    [Fact]
    public void Grade_PartialPass_UsesIntegerScoring()
    {
        var grade = Grade(
            ExecutionStatus.Succeeded,
            AssessmentTestOutput.BuildFromObservations(
                ("public-1", true, false),
                ("public-2", true, false),
                ("hidden-1", false, false)),
            Public("public-1"),
            Public("public-2"),
            Hidden("hidden-1"));

        Assert.Equal(AssessmentOutcome.WrongAnswer, grade.Assessment.Outcome);
        Assert.Equal(66, grade.Assessment.Score);
        Assert.Equal(2, grade.Assessment.PassedTests);
        Assert.Equal(3, grade.Assessment.TotalTests);
        Assert.Equal(2, grade.Assessment.PublicPassed);
        Assert.Equal(0, grade.Assessment.HiddenPassed);
        Assert.Equal(
            AssessmentTestStatus.Failed,
            grade.Assessment.TestResults.Single(result => result.TestId == "hidden-1").Status);
    }

    [Fact]
    public void Grade_AllTestsFail_ReturnsWrongAnswerWithZeroScore()
    {
        var grade = Grade(
            ExecutionStatus.Succeeded,
            AssessmentTestOutput.BuildFromObservations(
                ("public-1", false, false),
                ("hidden-1", false, false)),
            Public("public-1"),
            Hidden("hidden-1"));

        Assert.Equal(AssessmentOutcome.WrongAnswer, grade.Assessment.Outcome);
        Assert.Equal(0, grade.Assessment.Score);
        Assert.Equal(0, grade.Assessment.PassedTests);
        Assert.Equal(2, grade.Assessment.TotalTests);
    }

    [Fact]
    public void Grade_HarnessError_ReturnsRuntimeErrorAndKeepsScoring()
    {
        var grade = Grade(
            ExecutionStatus.Succeeded,
            AssessmentTestOutput.BuildFromObservations(
                ("public-1", true, false),
                ("hidden-1", false, true)),
            Public("public-1"),
            Hidden("hidden-1"));

        Assert.Equal(AssessmentOutcome.RuntimeError, grade.Assessment.Outcome);
        Assert.Equal(50, grade.Assessment.Score);
        Assert.Equal(1, grade.Assessment.PassedTests);
        Assert.Equal(
            AssessmentTestStatus.Failed,
            grade.Assessment.TestResults.Single(result => result.TestId == "hidden-1").Status);
    }

    [Theory]
    [InlineData(ExecutionStatus.CompilationError, AssessmentOutcome.CompilationError)]
    [InlineData(ExecutionStatus.RuntimeError, AssessmentOutcome.RuntimeError)]
    [InlineData(ExecutionStatus.TimeLimitExceeded, AssessmentOutcome.TimeLimitExceeded)]
    [InlineData(ExecutionStatus.MemoryLimitExceeded, AssessmentOutcome.MemoryLimitExceeded)]
    [InlineData(ExecutionStatus.OutputLimitExceeded, AssessmentOutcome.OutputLimitExceeded)]
    [InlineData(ExecutionStatus.ExecutionUnavailable, AssessmentOutcome.InternalRunnerError)]
    [InlineData(ExecutionStatus.InternalError, AssessmentOutcome.InternalRunnerError)]
    [InlineData(ExecutionStatus.Cancelled, AssessmentOutcome.InternalRunnerError)]
    public void Grade_NonSuccessfulExecution_MapsOutcomeAndScoresZero(
        ExecutionStatus status,
        AssessmentOutcome expected)
    {
        var grade = Grade(
            status,
            "partial output",
            Public("public-1"),
            Hidden("hidden-1"));

        Assert.Equal(expected, grade.Assessment.Outcome);
        Assert.Equal(0, grade.Assessment.Score);
        Assert.Equal(0, grade.Assessment.PassedTests);
        Assert.Equal(2, grade.Assessment.TotalTests);
        Assert.Equal(0, grade.Assessment.PublicPassed);
        Assert.Equal(0, grade.Assessment.HiddenPassed);
        Assert.All(
            grade.Assessment.TestResults,
            result => Assert.Equal(AssessmentTestStatus.NotRun, result.Status));
    }

    [Theory]
    [InlineData(AssessmentTestOutput.BeginMarker + "\nnot json\n" + AssessmentTestOutput.EndMarker)]
    [InlineData("no result block at all")]
    [InlineData(AssessmentTestOutput.BeginMarker + "\n[{\"id\":\"public-1\",\"passed\":true}]\n" + AssessmentTestOutput.EndMarker)]
    public void Grade_UnreadableOrIncompleteHarnessOutput_ReturnsInternalRunnerError(string output)
    {
        var grade = Grade(
            ExecutionStatus.Succeeded,
            output,
            Public("public-1"),
            Public("public-2"));

        Assert.Equal(AssessmentOutcome.InternalRunnerError, grade.Assessment.Outcome);
        Assert.Equal(0, grade.Assessment.Score);
        Assert.Equal(2, grade.Assessment.TotalTests);
        Assert.All(
            grade.Assessment.TestResults,
            result => Assert.Equal(AssessmentTestStatus.NotRun, result.Status));
    }

    [Fact]
    public void Grade_DuplicateObservation_ReturnsInternalRunnerError()
    {
        var grade = Grade(
            ExecutionStatus.Succeeded,
            AssessmentTestOutput.BuildFromObservations(
                ("public-1", true, false),
                ("public-1", true, false)),
            Public("public-1"),
            Public("public-2"));

        Assert.Equal(AssessmentOutcome.InternalRunnerError, grade.Assessment.Outcome);
    }

    [Fact]
    public void Grade_UnexpectedObservation_ReturnsInternalRunnerError()
    {
        var grade = Grade(
            ExecutionStatus.Succeeded,
            AssessmentTestOutput.BuildFromObservations(
                ("public-1", true, false),
                ("hidden-1", true, false),
                ("hidden-2", true, false)),
            Public("public-1"),
            Public("public-2"));

        Assert.Equal(AssessmentOutcome.InternalRunnerError, grade.Assessment.Outcome);
    }

    [Fact]
    public void Grade_NoSelectedTests_ReturnsInternalRunnerError()
    {
        var grade = Grade(
            ExecutionStatus.Succeeded,
            AssessmentTestOutput.BuildFromObservations(),
            []);

        Assert.Equal(AssessmentOutcome.InternalRunnerError, grade.Assessment.Outcome);
        Assert.Equal(0, grade.Assessment.Score);
        Assert.Equal(0, grade.Assessment.TotalTests);
    }

    [Fact]
    public void Grade_StripsHarnessBlockFromClientOutput()
    {
        var output = string.Concat(
            "before\n",
            AssessmentTestOutput.BuildFromObservations(("public-1", true, false)),
            "after\n");

        var grade = Grade(
            ExecutionStatus.Succeeded,
            output,
            Public("public-1"));

        Assert.DoesNotContain(AssessmentTestOutput.BeginMarker, grade.SanitizedStandardOutput);
        Assert.DoesNotContain(AssessmentTestOutput.EndMarker, grade.SanitizedStandardOutput);
        Assert.Equal("before\n\nafter", grade.SanitizedStandardOutput);
    }

    [Fact]
    public void Grade_OutputWithoutResultBlockIsUnchanged()
    {
        var grade = Grade(
            ExecutionStatus.CompilationError,
            "compiler diagnostics",
            Public("public-1"));

        Assert.Equal("compiler diagnostics", grade.SanitizedStandardOutput);
    }

    [Fact]
    public void Grade_IsDeterministicForTheSameInput()
    {
        var tests = new[]
        {
            Public("public-1"),
            Public("public-2"),
            Hidden("hidden-1")
        };
        var output = AssessmentTestOutput.BuildFromObservations(
            ("public-1", true, false),
            ("public-2", false, false),
            ("hidden-1", true, false));

        var first = Grade(ExecutionStatus.Succeeded, output, tests);
        var second = Grade(ExecutionStatus.Succeeded, output, tests);

        Assert.Equal(first.Assessment.Outcome, second.Assessment.Outcome);
        Assert.Equal(first.Assessment.Score, second.Assessment.Score);
        Assert.Equal(first.Assessment.PassedTests, second.Assessment.PassedTests);
        Assert.Equal(first.Assessment.PublicPassed, second.Assessment.PublicPassed);
        Assert.Equal(first.Assessment.HiddenPassed, second.Assessment.HiddenPassed);
        Assert.Equal(
            first.Assessment.TestResults.Select(result => result.Status),
            second.Assessment.TestResults.Select(result => result.Status));
    }

    [Fact]
    public void Grade_PreservesTestSelectionOrderInEvidence()
    {
        var grade = Grade(
            ExecutionStatus.Succeeded,
            AssessmentTestOutput.BuildFromObservations(
                ("hidden-1", true, false),
                ("public-1", true, false)),
            Public("public-1"),
            Hidden("hidden-1"));

        Assert.Equal(
            ["public-1", "hidden-1"],
            grade.Assessment.TestResults
                .OrderBy(result => result.SortOrder)
                .Select(result => result.TestId));
        Assert.Equal(AssessmentVisibility.Public, grade.Assessment.TestResults
            .Single(result => result.TestId == "public-1").Visibility);
        Assert.Equal(AssessmentVisibility.Hidden, grade.Assessment.TestResults
            .Single(result => result.TestId == "hidden-1").Visibility);
    }

    private static AssessmentGrade Grade(
        ExecutionStatus status,
        string output,
        params AssessmentTestDefinition[] tests)
    {
        return AssessmentGrader.Grade(new AssessmentInput(
            SubmissionId,
            "two-sum",
            "csharp",
            "Submit",
            status,
            output,
            tests));
    }

    private static AssessmentTestDefinition Public(string id)
    {
        return new AssessmentTestDefinition(id, true, $"Public {id}", "Public description");
    }

    private static AssessmentTestDefinition Hidden(string id)
    {
        return new AssessmentTestDefinition(id, false, $"Hidden {id}", "Hidden description");
    }
}
