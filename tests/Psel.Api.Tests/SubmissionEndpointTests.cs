using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Psel.Modules.Execution;

namespace Psel.Api.Tests;

public sealed class SubmissionEndpointTests
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task PostSubmission_ExecutesWithServerHarnessAndReturnsOutput()
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
                files = new[]
                {
                    new
                    {
                        path = "Solution.cs",
                        content = "namespace Challenge; public static class Solution { }"
                    }
                }
            },
            cancellationToken);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var created = await response.Content.ReadFromJsonAsync<SubmissionCreatedDto>(
            JsonOptions,
            cancellationToken);
        Assert.NotNull(created);
        Assert.Equal("Queued", created.Status);
        Assert.Contains(created.Id.ToString(), response.Headers.Location!.ToString());

        var detail = await WaitForTerminalAsync(client, created.Id, cancellationToken);

        Assert.Equal("Succeeded", detail.Status);
        Assert.True(detail.IsTerminal);
        Assert.Contains("fake output", detail.StandardOutput, StringComparison.Ordinal);
        Assert.Equal(0, detail.ExitCode);

        var request = Assert.Single(factory.Runner.Requests);
        Assert.Equal("two-sum", request.ChallengeSlug);
        Assert.Equal("csharp", request.LanguageId);
        Assert.Equal(created.Id, request.SubmissionId);
        Assert.Equal("Solution.cs", Assert.Single(request.Files).Path);
        Assert.False(string.IsNullOrWhiteSpace(request.HarnessSource));
        Assert.Contains("Solution.TwoSum", request.HarnessSource, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetSubmission_UnknownId_ReturnsNotFoundProblem()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(
            $"/api/submissions/{Guid.NewGuid()}",
            cancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task PostSubmission_UnknownChallenge_ReturnsBadRequest()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        using var response = await client.PostAsJsonAsync(
            "/api/submissions",
            new
            {
                challengeSlug = "does-not-exist",
                languageId = "csharp",
                files = new[] { new { path = "Solution.cs", content = "// code" } }
            },
            cancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(factory.Runner.Requests);
    }

    [Fact]
    public async Task PostSubmission_ChallengeWithoutHarness_ReturnsBadRequest()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        using var response = await client.PostAsJsonAsync(
            "/api/submissions",
            new
            {
                challengeSlug = "api-validation",
                languageId = "csharp",
                files = new[] { new { path = "Program.cs", content = "// code" } }
            },
            cancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        Assert.Contains("cannot be executed", body, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(factory.Runner.Requests);
    }

    [Fact]
    public async Task PostSubmission_PythonExecutesWithServerHarnessAndGrades()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CustomWebApplicationFactory();
        factory.PythonRunner.Handler = request =>
            AssessmentTestOutput.Success(AssessmentTestOutput.Build(request));
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/api/submissions",
            new
            {
                challengeSlug = "two-sum",
                languageId = "python",
                purpose = "Submit",
                files = new[]
                {
                    new
                    {
                        path = "solution.py",
                        content = "def two_sum(numbers, target):\n    return [0, 1]\n"
                    }
                }
            },
            cancellationToken);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<SubmissionCreatedDto>(
            JsonOptions,
            cancellationToken);
        Assert.NotNull(created);

        var detail = await WaitForTerminalAsync(client, created.Id, cancellationToken);

        Assert.Equal("Succeeded", detail.Status);
        Assert.Equal("python", detail.LanguageId);

        var request = Assert.Single(factory.PythonRunner.Requests);
        Assert.Equal("python", request.LanguageId);
        Assert.Equal("solution.py", Assert.Single(request.Files).Path);
        Assert.Contains("import solution", request.HarnessSource, StringComparison.Ordinal);
        Assert.NotNull(request.Tests);
        Assert.Equal(4, request.Tests.Count);
        Assert.Empty(factory.Runner.Requests);

        using var resultResponse = await client.GetAsync(
            $"/api/submissions/{created.Id}/results",
            cancellationToken);
        using var result = JsonDocument.Parse(
            await resultResponse.Content.ReadAsStringAsync(cancellationToken));

        Assert.Equal(HttpStatusCode.OK, resultResponse.StatusCode);
        Assert.Equal("Accepted", result.RootElement.GetProperty("outcome").GetString());
        Assert.Equal(4, result.RootElement.GetProperty("totalTests").GetInt32());
    }

    [Theory]
    [InlineData("../escape.cs")]
    [InlineData("/absolute.cs")]
    [InlineData("nested/../../escape.cs")]
    public async Task PostSubmission_UnsafePath_ReturnsBadRequest(string path)
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        using var response = await client.PostAsJsonAsync(
            "/api/submissions",
            new
            {
                challengeSlug = "two-sum",
                languageId = "csharp",
                files = new[] { new { path, content = "// code" } }
            },
            cancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(factory.Runner.Requests);
    }

    [Fact]
    public async Task PostSubmission_UnsupportedExtension_ReturnsBadRequest()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        using var response = await client.PostAsJsonAsync(
            "/api/submissions",
            new
            {
                challengeSlug = "two-sum",
                languageId = "csharp",
                files = new[] { new { path = "Solution.txt", content = "// code" } }
            },
            cancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(factory.Runner.Requests);
    }

    [Fact]
    public async Task PostSubmission_RunnerUnavailable_IsPersistedAsUnavailable()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CustomWebApplicationFactory();
        factory.Runner.Handler = _ => new ExecutionOutcome(
            ExecutionStatus.ExecutionUnavailable,
            null,
            0,
            string.Empty,
            string.Empty,
            "Docker is not available.",
            false);
        using var client = factory.CreateClient();

        var created = await PostValidSubmissionAsync(client, cancellationToken);
        var detail = await WaitForTerminalAsync(client, created.Id, cancellationToken);

        Assert.Equal("ExecutionUnavailable", detail.Status);
        Assert.Equal("Docker is not available.", detail.FailureMessage);
    }

    [Fact]
    public async Task PostSubmission_RuntimeFailureMapsToRuntimeError()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CustomWebApplicationFactory();
        factory.Runner.Handler = _ => new ExecutionOutcome(
            ExecutionStatus.RuntimeError,
            1,
            34,
            "partial output",
            "boom",
            "The program exited with code 1.",
            false);
        using var client = factory.CreateClient();

        var created = await PostValidSubmissionAsync(client, cancellationToken);
        var detail = await WaitForTerminalAsync(client, created.Id, cancellationToken);

        Assert.Equal("RuntimeError", detail.Status);
        Assert.Equal(1, detail.ExitCode);
        Assert.Equal("partial output", detail.StandardOutput);
        Assert.Equal("boom", detail.StandardError);
    }

    [Fact]
    public async Task GetChallenge_DoesNotExposeExecutionHarness()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/challenges/two-sum", cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain("PselHarness", body, StringComparison.Ordinal);
        Assert.DoesNotContain(
            "Server-side execution harness",
            body,
            StringComparison.Ordinal);
        Assert.DoesNotContain("Solution.TwoSum", body, StringComparison.Ordinal);
        Assert.Contains("\"canExecute\":true", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetLanguages_ReturnsServerConfiguredLanguages()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/languages", cancellationToken);
        var languages = await response.Content.ReadFromJsonAsync<List<LanguageDto>>(
            JsonOptions,
            cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(languages);
        Assert.Contains(languages, language => language.Id == "csharp");
        Assert.Contains(languages, language => language.Id == "python");
        Assert.Contains(languages, language => language.Id == "sql");
    }

    [Fact]
    public async Task PostSubmission_SqlUsesBuiltInHarnessAndServerOwnedData()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CustomWebApplicationFactory();
        factory.SqlRunner.Handler = request =>
            AssessmentTestOutput.Success(AssessmentTestOutput.Build(request));
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/api/submissions",
            new
            {
                challengeSlug = "order-revenue",
                languageId = "sql",
                purpose = "Submit",
                files = new[]
                {
                    new
                    {
                        path = "query.sql",
                        content = "SELECT c.name, 0, 0.0 FROM customers AS c;"
                    }
                }
            },
            cancellationToken);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<SubmissionCreatedDto>(
            JsonOptions,
            cancellationToken);
        Assert.NotNull(created);

        var detail = await WaitForTerminalAsync(client, created.Id, cancellationToken);
        Assert.Equal("Succeeded", detail.Status);
        Assert.Equal("sql", detail.LanguageId);

        var request = Assert.Single(factory.SqlRunner.Requests);
        Assert.Equal("sql", request.LanguageId);
        Assert.Equal("query.sql", Assert.Single(request.Files).Path);
        Assert.Contains("__PSEL_RESULTS_BEGIN__", request.HarnessSource, StringComparison.Ordinal);
        Assert.NotNull(request.Tests);
        Assert.Equal(5, request.Tests.Count);

        var firstTest = request.Tests[0];
        Assert.Contains("\"schema\"", firstTest.DataJson, StringComparison.Ordinal);
        Assert.Contains("CREATE TABLE customers", firstTest.DataJson, StringComparison.Ordinal);
        Assert.Contains("\"seed\"", firstTest.DataJson, StringComparison.Ordinal);
        Assert.Empty(factory.Runner.Requests);
        Assert.Empty(factory.PythonRunner.Requests);

        using var resultResponse = await client.GetAsync(
            $"/api/submissions/{created.Id}/results",
            cancellationToken);
        using var result = JsonDocument.Parse(
            await resultResponse.Content.ReadAsStringAsync(cancellationToken));

        Assert.Equal(HttpStatusCode.OK, resultResponse.StatusCode);
        Assert.Equal("Accepted", result.RootElement.GetProperty("outcome").GetString());
        Assert.Equal(5, result.RootElement.GetProperty("totalTests").GetInt32());
    }

    [Fact]
    public async Task PostSubmission_SqlWithMultipleFiles_ReturnsBadRequest()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        using var response = await client.PostAsJsonAsync(
            "/api/submissions",
            new
            {
                challengeSlug = "order-revenue",
                languageId = "sql",
                files = new[]
                {
                    new { path = "query.sql", content = "SELECT 1;" },
                    new { path = "extra.sql", content = "SELECT 2;" }
                }
            },
            cancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(factory.SqlRunner.Requests);
    }

    [Fact]
    public async Task GetSqlChallenge_DoesNotExposeSchemaSeedOrHiddenTests()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(
            "/api/challenges/order-index-plan",
            cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"editorLanguage\":\"sql\"", body, StringComparison.Ordinal);
        Assert.Contains("\"canExecute\":true", body, StringComparison.Ordinal);
        Assert.DoesNotContain("\"schema\"", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"seed\"", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("cutoff-boundary", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("other-customers", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("2025-12-31", body, StringComparison.Ordinal);
    }

    private static Task<SubmissionCreatedDto> PostValidSubmissionAsync(
        HttpClient client,
        CancellationToken cancellationToken)
    {
        return PostValidSubmissionAsync(client, cancellationToken, "// code");
    }

    private static async Task<SubmissionCreatedDto> PostValidSubmissionAsync(
        HttpClient client,
        CancellationToken cancellationToken,
        string content)
    {
        using var response = await client.PostAsJsonAsync(
            "/api/submissions",
            new
            {
                challengeSlug = "two-sum",
                languageId = "csharp",
                files = new[] { new { path = "Solution.cs", content } }
            },
            cancellationToken);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<SubmissionCreatedDto>(
            JsonOptions,
            cancellationToken);

        Assert.NotNull(created);
        return created;
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

    private sealed record LanguageDto(string Id, string Name);

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
        bool OutputTruncated);
}
