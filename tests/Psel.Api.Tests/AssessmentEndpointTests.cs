using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Psel.Modules.Execution;

namespace Psel.Api.Tests;

public sealed class AssessmentEndpointTests
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task Run_ExecutesOnlyPublicTestsAndScoresThem()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CustomWebApplicationFactory();
        factory.Runner.Handler = request =>
            AssessmentTestOutput.Success(AssessmentTestOutput.Build(request));
        using var client = factory.CreateClient();

        var created = await PostSubmissionAsync(client, "Run", cancellationToken);
        var detail = await WaitForTerminalAsync(client, created.Id, cancellationToken);
        var result = await GetResultAsync(client, created.Id, cancellationToken);

        Assert.Equal("Succeeded", detail.Status);
        Assert.Equal("Run", detail.Purpose);
        Assert.Equal("Run", result.Purpose);
        Assert.Equal("Accepted", result.Outcome);
        Assert.True(result.IsAccepted);
        Assert.Equal(100, result.Score);
        Assert.Equal(2, result.TotalTests);
        Assert.Equal(2, result.PublicTotal);
        Assert.Equal(0, result.HiddenTotal);

        var request = Assert.Single(factory.Runner.Requests);
        Assert.NotNull(request.Tests);
        Assert.Equal(2, request.Tests.Count);
        Assert.All(
            request.Tests,
            test => Assert.Equal("public", test.Visibility));

        Assert.DoesNotContain(
            AssessmentTestOutput.BeginMarker,
            detail.StandardOutput,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            AssessmentTestOutput.EndMarker,
            detail.StandardOutput,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task Submit_ExecutesPublicAndHiddenTestsWithoutDisclosingHiddenMaterial()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CustomWebApplicationFactory();
        factory.Runner.Handler = request =>
            AssessmentTestOutput.Success(AssessmentTestOutput.Build(request));
        using var client = factory.CreateClient();

        var created = await PostSubmissionAsync(client, "Submit", cancellationToken);
        await WaitForTerminalAsync(client, created.Id, cancellationToken);

        var request = Assert.Single(factory.Runner.Requests);
        Assert.NotNull(request.Tests);
        Assert.Equal(4, request.Tests.Count);
        Assert.Equal(
            ["public-1", "public-2", "hidden-1", "hidden-2"],
            request.Tests.Select(test => test.Id));
        Assert.Equal(
            ["public", "public", "hidden", "hidden"],
            request.Tests.Select(test => test.Visibility));

        using var response = await client.GetAsync(
            $"/api/submissions/{created.Id}/results",
            cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"hiddenTotal\":2", body, StringComparison.Ordinal);
        Assert.Contains("\"publicTotal\":2", body, StringComparison.Ordinal);
        Assert.Contains("Example 1", body, StringComparison.Ordinal);
        Assert.Contains("Example 2", body, StringComparison.Ordinal);

        foreach (var hiddenValue in new[]
        {
            "Duplicate values",
            "Negative numbers",
            "Repeated values must still produce two distinct indices.",
            "The target can be reached with negative values.",
            "hidden-1",
            "hidden-2"
        })
        {
            Assert.DoesNotContain(hiddenValue, body, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task Run_WithAFailingTest_ReturnsWrongAnswerWithProportionalScore()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CustomWebApplicationFactory();
        factory.Runner.Handler = request =>
            AssessmentTestOutput.Success(AssessmentTestOutput.Build(
                request,
                test => (test.Id != "public-2", false)));
        using var client = factory.CreateClient();

        var created = await PostSubmissionAsync(client, "Run", cancellationToken);
        var result = await GetResultAsync(
            client,
            created.Id,
            cancellationToken,
            terminal: true);

        Assert.Equal("WrongAnswer", result.Outcome);
        Assert.False(result.IsAccepted);
        Assert.Equal(50, result.Score);
        Assert.Equal(1, result.PassedTests);
        Assert.Equal(1, result.PublicPassed);
        Assert.Equal(2, result.TotalTests);
        Assert.Contains(
            result.PublicTests,
            test => test.Status == "Failed" && test.Name == "Example 2");
    }

    [Fact]
    public async Task Submit_WhenHarnessReportsATestError_ReturnsRuntimeError()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CustomWebApplicationFactory();
        factory.Runner.Handler = request =>
            AssessmentTestOutput.Success(AssessmentTestOutput.Build(
                request,
                test => test.Id == "public-1" ? (true, false) : (false, true)));
        using var client = factory.CreateClient();

        var created = await PostSubmissionAsync(client, "Submit", cancellationToken);
        var detail = await WaitForTerminalAsync(client, created.Id, cancellationToken);
        var result = await GetResultAsync(client, created.Id, cancellationToken);

        Assert.Equal("Succeeded", detail.Status);
        Assert.Equal("RuntimeError", result.Outcome);
        Assert.Equal(25, result.Score);
        Assert.Equal(1, result.PassedTests);
        Assert.Equal(4, result.TotalTests);
    }

    [Fact]
    public async Task Submit_WhenExecutionFails_ScoresZeroAndMarksTestsNotRun()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CustomWebApplicationFactory();
        factory.Runner.Handler = _ => new ExecutionOutcome(
            ExecutionStatus.CompilationError,
            1,
            40,
            string.Empty,
            "Solution.cs(1,1): error CS1002: ; expected",
            "Compilation failed.",
            false);
        using var client = factory.CreateClient();

        var created = await PostSubmissionAsync(client, "Submit", cancellationToken);
        var detail = await WaitForTerminalAsync(client, created.Id, cancellationToken);
        var result = await GetResultAsync(client, created.Id, cancellationToken);

        Assert.Equal("CompilationError", detail.Status);
        Assert.Equal("CompilationError", result.Outcome);
        Assert.Equal(0, result.Score);
        Assert.Equal(0, result.PassedTests);
        Assert.Equal(4, result.TotalTests);
        Assert.All(result.PublicTests, test => Assert.Equal("NotRun", test.Status));
    }

    [Fact]
    public async Task GetResult_UnknownSubmission_ReturnsNotFoundProblem()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(
            $"/api/submissions/{Guid.NewGuid()}/results",
            cancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task PostSubmission_InvalidPurpose_ReturnsBadRequest()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/api/submissions",
            new
            {
                challengeSlug = "two-sum",
                languageId = "csharp",
                purpose = "GradeLater",
                files = new[] { new { path = "Solution.cs", content = "// code" } }
            },
            cancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(factory.Runner.Requests);
    }

    [Theory]
    [InlineData("PselHarness.cs")]
    [InlineData("PselTests.json")]
    [InlineData("Submission.csproj")]
    public async Task PostSubmission_ReservedRunnerFile_ReturnsBadRequest(string path)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/api/submissions",
            new
            {
                challengeSlug = "two-sum",
                languageId = "csharp",
                purpose = "Submit",
                files = new[] { new { path, content = "// code" } }
            },
            cancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(factory.Runner.Requests);
    }

    private static async Task<SubmissionCreatedDto> PostSubmissionAsync(
        HttpClient client,
        string purpose,
        CancellationToken cancellationToken)
    {
        using var response = await client.PostAsJsonAsync(
            "/api/submissions",
            new
            {
                challengeSlug = "two-sum",
                languageId = "csharp",
                purpose,
                files = new[] { new { path = "Solution.cs", content = "// code" } }
            },
            cancellationToken);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<SubmissionCreatedDto>(
            JsonOptions,
            cancellationToken);

        Assert.NotNull(created);
        return created;
    }

    private static async Task<AssessmentResultDto> GetResultAsync(
        HttpClient client,
        Guid id,
        CancellationToken cancellationToken,
        bool terminal = false)
    {
        if (terminal)
        {
            await WaitForTerminalAsync(client, id, cancellationToken);
        }

        var result = await client.GetFromJsonAsync<AssessmentResultDto>(
            $"/api/submissions/{id}/results",
            JsonOptions,
            cancellationToken);

        Assert.NotNull(result);
        return result;
    }

    private static async Task<SubmissionDetailDto> WaitForTerminalAsync(
        HttpClient client,
        Guid id,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 100; attempt++)
        {
            var detail = await client.GetFromJsonAsync<SubmissionDetailDto>(
                $"/api/submissions/{id}",
                JsonOptions,
                cancellationToken);

            if (detail is not null && detail.IsTerminal)
            {
                return detail;
            }

            await Task.Delay(50, cancellationToken);
        }

        throw new InvalidOperationException(
            $"Submission {id} did not reach a terminal state in time.");
    }

    private sealed record SubmissionCreatedDto(
        Guid Id,
        string Status,
        DateTimeOffset CreatedAtUtc);

    private sealed record SubmissionDetailDto(
        Guid Id,
        string ChallengeSlug,
        string LanguageId,
        string Status,
        bool IsTerminal,
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset? StartedAtUtc,
        DateTimeOffset? CompletedAtUtc,
        long? DurationMs,
        int? ExitCode,
        string StandardOutput,
        string StandardError,
        string? FailureMessage,
        bool OutputTruncated,
        string Purpose);

    private sealed record AssessmentTestDto(
        string Name,
        string? Description,
        string Status);

    private sealed record AssessmentResultDto(
        Guid SubmissionId,
        string ChallengeSlug,
        string LanguageId,
        string Purpose,
        string Outcome,
        bool IsAccepted,
        int Score,
        int PassedTests,
        int TotalTests,
        int PublicPassed,
        int PublicTotal,
        int HiddenPassed,
        int HiddenTotal,
        List<AssessmentTestDto> PublicTests,
        DateTimeOffset CreatedAtUtc);
}
