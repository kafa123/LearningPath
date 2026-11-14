using System.Net;
using System.Net.Http.Json;

namespace Psel.Api.Tests;

public sealed class ExaminationEndpointTests
{
    [Fact]
    public async Task Create_ReturnsServerSelectedVariantAndHintFreeChallenge()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        var detail = await Phase7TestSupport.CreateExaminationAsync(
            client,
            "two-sum",
            cancellationToken);

        Assert.Equal("two-sum", detail.ChallengeSlug);
        Assert.Equal("Two Sum", detail.ChallengeTitle);
        Assert.Contains(detail.VariantId, new[] { "standard", "extended" });
        Assert.Equal(detail.VariantId == "standard" ? 600 : 900, detail.TimeLimitSeconds);
        Assert.Equal("InProgress", detail.Status);
        Assert.True(detail.IsActive);
        Assert.NotNull(detail.ExpiresAtUtc);
        Assert.NotNull(detail.RemainingSeconds);
        Assert.InRange(
            detail.RemainingSeconds!.Value,
            1,
            detail.TimeLimitSeconds!.Value);
        Assert.Equal("two-sum", detail.Challenge.Slug);
        Assert.Null(detail.Challenge.HintsMarkdown);
    }

    [Fact]
    public async Task Create_UnknownChallenge_ReturnsBadRequest()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/api/examinations",
            new { challengeSlug = "does-not-exist" },
            cancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Get_UnknownExamination_ReturnsNotFound()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(
            $"/api/examinations/{Guid.NewGuid()}",
            cancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Submit_WithRunPurposeForExamination_ReturnsBadRequest()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();
        var examination = await Phase7TestSupport.CreateExaminationAsync(
            client,
            "two-sum",
            cancellationToken);

        using var response = await client.PostAsJsonAsync(
            "/api/submissions",
            new
            {
                challengeSlug = "two-sum",
                languageId = "csharp",
                purpose = "Run",
                examinationId = examination.Id,
                files = new[] { new { path = "Solution.cs", content = "// code" } }
            },
            cancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(factory.Runner.Requests);
    }

    [Fact]
    public async Task Submit_ForDifferentChallenge_ReturnsBadRequest()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();
        var examination = await Phase7TestSupport.CreateExaminationAsync(
            client,
            "two-sum",
            cancellationToken);

        using var response = await client.PostAsJsonAsync(
            "/api/submissions",
            new
            {
                challengeSlug = "balanced-brackets",
                languageId = "csharp",
                purpose = "Submit",
                examinationId = examination.Id,
                files = new[] { new { path = "Solution.cs", content = "// code" } }
            },
            cancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(factory.Runner.Requests);
    }

    [Fact]
    public async Task Submit_AfterExpiry_ReturnsBadRequestAndMarksSessionExpired()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();
        var examination = await Phase7TestSupport.CreateExaminationAsync(
            client,
            "two-sum",
            cancellationToken);

        factory.Clock.Advance(TimeSpan.FromHours(2));

        using var response = await client.PostAsJsonAsync(
            "/api/submissions",
            new
            {
                challengeSlug = "two-sum",
                languageId = "csharp",
                purpose = "Submit",
                examinationId = examination.Id,
                files = new[] { new { path = "Solution.cs", content = "// code" } }
            },
            cancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(factory.Runner.Requests);

        var fetched = await client.GetFromJsonAsync<ExaminationDetailDto>(
            $"/api/examinations/{examination.Id}",
            Phase7TestSupport.JsonOptions,
            cancellationToken);

        Assert.NotNull(fetched);
        Assert.Equal("Expired", fetched.Status);
        Assert.False(fetched.IsActive);
        Assert.Equal(0, fetched.RemainingSeconds);
    }

    [Fact]
    public async Task Finish_CompletesSessionAndRejectsFurtherSubmission()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();
        var examination = await Phase7TestSupport.CreateExaminationAsync(
            client,
            "two-sum",
            cancellationToken);

        using var finishResponse = await client.PostAsync(
            $"/api/examinations/{examination.Id}/finish",
            null,
            cancellationToken);

        Assert.Equal(HttpStatusCode.OK, finishResponse.StatusCode);
        var finished = await finishResponse.Content.ReadFromJsonAsync<ExaminationDetailDto>(
            Phase7TestSupport.JsonOptions,
            cancellationToken);

        Assert.NotNull(finished);
        Assert.Equal("Completed", finished.Status);
        Assert.False(finished.IsActive);
        Assert.NotNull(finished.CompletedAtUtc);

        using var submissionResponse = await client.PostAsJsonAsync(
            "/api/submissions",
            new
            {
                challengeSlug = "two-sum",
                languageId = "csharp",
                purpose = "Submit",
                examinationId = examination.Id,
                files = new[] { new { path = "Solution.cs", content = "// code" } }
            },
            cancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, submissionResponse.StatusCode);
        Assert.Empty(factory.Runner.Requests);
    }

    [Fact]
    public async Task List_ReturnsSessionsAndTracksAttemptCount()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CustomWebApplicationFactory();
        factory.Runner.Handler = request =>
            AssessmentTestOutput.Success(AssessmentTestOutput.Build(request));
        using var client = factory.CreateClient();
        var examination = await Phase7TestSupport.CreateExaminationAsync(
            client,
            "two-sum",
            cancellationToken);

        var initial = await client.GetFromJsonAsync<List<ExaminationSummaryDto>>(
            "/api/examinations",
            Phase7TestSupport.JsonOptions,
            cancellationToken);

        Assert.NotNull(initial);
        var summary = Assert.Single(initial);
        Assert.Equal(examination.Id, summary.Id);
        Assert.Equal("InProgress", summary.Status);
        Assert.Equal(0, summary.AttemptCount);

        var attempt = await Phase7TestSupport.PostSubmissionAsync(
            client,
            "two-sum",
            "Submit",
            examination.Id,
            cancellationToken);
        await Phase7TestSupport.WaitForTerminalAsync(client, attempt.Id, cancellationToken);

        var updated = await client.GetFromJsonAsync<List<ExaminationSummaryDto>>(
            "/api/examinations",
            Phase7TestSupport.JsonOptions,
            cancellationToken);

        Assert.NotNull(updated);
        Assert.Equal(1, Assert.Single(updated).AttemptCount);
    }
}
