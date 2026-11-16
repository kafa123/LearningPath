using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Psel.Api.Tests;

public sealed class RubricEndpointTests
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task GetRubricReview_WhenNoneExists_ReturnsNotFoundProblem()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        var submissionId = await CreateTerminalSubmissionAsync(client, cancellationToken);

        using var response = await client.GetAsync(
            $"/api/submissions/{submissionId}/rubric",
            cancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        Assert.Contains("Rubric review not found.", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PutRubricReview_ComputesWeightedScoreAndUpserts()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        var submissionId = await CreateTerminalSubmissionAsync(client, cancellationToken);

        using var firstResponse = await client.PutAsJsonAsync(
            $"/api/submissions/{submissionId}/rubric",
            new
            {
                criteria = new[]
                {
                    new { id = "behavior-preservation", points = 10 },
                    new { id = "single-source-of-truth", points = 8 },
                    new { id = "clarity", points = 7 },
                    new { id = "test-confidence", points = 9 }
                },
                note = "  Extracted the shared rule.  "
            },
            cancellationToken);

        var first = await firstResponse.Content.ReadFromJsonAsync<RubricReviewDto>(
            JsonOptions,
            cancellationToken);

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.NotNull(first);
        Assert.Equal(submissionId, first.SubmissionId);
        Assert.Equal("refactor-duplication", first.ChallengeSlug);
        Assert.Equal(86, first.Score);
        Assert.Equal("Exemplary", first.Band);
        Assert.Equal("Extracted the shared rule.", first.Note);
        Assert.Equal(
            ["behavior-preservation", "single-source-of-truth", "clarity", "test-confidence"],
            first.Criteria.Select(criterion => criterion.Id));
        Assert.Equal(10, first.Criteria[0].MaxPoints);
        Assert.Equal(3, first.Criteria[0].Weight);
        Assert.Equal(10, first.Criteria[0].Points);
        Assert.Equal("Behavior preservation", first.Criteria[0].Title);
        Assert.False(string.IsNullOrWhiteSpace(first.Criteria[0].Description));

        using var getResponse = await client.GetAsync(
            $"/api/submissions/{submissionId}/rubric",
            cancellationToken);
        var stored = await getResponse.Content.ReadFromJsonAsync<RubricReviewDto>(
            JsonOptions,
            cancellationToken);

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        Assert.NotNull(stored);
        Assert.Equal(86, stored.Score);
        Assert.Equal("Exemplary", stored.Band);
        Assert.Equal(first.Criteria, stored.Criteria);

        using var secondResponse = await client.PutAsJsonAsync(
            $"/api/submissions/{submissionId}/rubric",
            new
            {
                criteria = new[]
                {
                    new { id = "behavior-preservation", points = 5 },
                    new { id = "single-source-of-truth", points = 5 },
                    new { id = "clarity", points = 5 },
                    new { id = "test-confidence", points = 5 }
                },
                note = (string?)null
            },
            cancellationToken);

        var second = await secondResponse.Content.ReadFromJsonAsync<RubricReviewDto>(
            JsonOptions,
            cancellationToken);

        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
        Assert.NotNull(second);
        Assert.Equal(50, second.Score);
        Assert.Equal("Developing", second.Band);
        Assert.Null(second.Note);
        Assert.All(second.Criteria, criterion => Assert.Equal(5, criterion.Points));
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("duplicate")]
    [InlineData("out-of-range")]
    [InlineData("unknown")]
    public async Task PutRubricReview_InvalidCriteria_ReturnsBadRequest(string kind)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        var submissionId = await CreateTerminalSubmissionAsync(client, cancellationToken);

        object criteria = kind switch
        {
            "missing" => new[] { new { id = "clarity", points = 5 } },
            "duplicate" => new[]
            {
                new { id = "behavior-preservation", points = 5 },
                new { id = "behavior-preservation", points = 6 },
                new { id = "clarity", points = 5 },
                new { id = "test-confidence", points = 5 }
            },
            "out-of-range" => new[]
            {
                new { id = "behavior-preservation", points = 11 },
                new { id = "single-source-of-truth", points = 5 },
                new { id = "clarity", points = 5 },
                new { id = "test-confidence", points = 5 }
            },
            _ => new[]
            {
                new { id = "behavior-preservation", points = 5 },
                new { id = "single-source-of-truth", points = 5 },
                new { id = "clarity", points = 5 },
                new { id = "test-confidence", points = 5 },
                new { id = "unknown-criterion", points = 5 }
            }
        };

        using var response = await client.PutAsJsonAsync(
            $"/api/submissions/{submissionId}/rubric",
            new { criteria, note = "Review" },
            cancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var stillMissing = await client.GetAsync(
            $"/api/submissions/{submissionId}/rubric",
            cancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, stillMissing.StatusCode);
    }

    [Fact]
    public async Task PutRubricReview_UnknownSubmission_ReturnsNotFound()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        using var response = await client.PutAsJsonAsync(
            $"/api/submissions/{Guid.NewGuid()}/rubric",
            new { criteria = Array.Empty<object>(), note = (string?)null },
            cancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PutRubricReview_ChallengeWithoutRubric_ReturnsBadRequest()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        var submissionId = await CreateSubmissionAsync(
            client,
            "two-sum",
            "csharp",
            "Solution.cs",
            cancellationToken);

        using var response = await client.PutAsJsonAsync(
            $"/api/submissions/{submissionId}/rubric",
            new { criteria = Array.Empty<object>(), note = (string?)null },
            cancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        Assert.Contains("does not define a rubric", body, StringComparison.Ordinal);
    }

    private static Task<Guid> CreateTerminalSubmissionAsync(
        HttpClient client,
        CancellationToken cancellationToken)
    {
        return CreateSubmissionAsync(
            client,
            "refactor-duplication",
            "csharp",
            "Solution.cs",
            cancellationToken);
    }

    private static async Task<Guid> CreateSubmissionAsync(
        HttpClient client,
        string challengeSlug,
        string languageId,
        string filePath,
        CancellationToken cancellationToken)
    {
        using var response = await client.PostAsJsonAsync(
            "/api/submissions",
            new
            {
                challengeSlug,
                languageId,
                files = new[] { new { path = filePath, content = "// code" } }
            },
            cancellationToken);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<SubmissionCreatedDto>(
            JsonOptions,
            cancellationToken);

        Assert.NotNull(created);

        for (var attempt = 0; attempt < 100; attempt++)
        {
            var detail = await client.GetFromJsonAsync<SubmissionDetailDto>(
                $"/api/submissions/{created.Id}",
                JsonOptions,
                cancellationToken);

            if (detail is not null && detail.IsTerminal)
            {
                return created.Id;
            }

            await Task.Delay(50, cancellationToken);
        }

        throw new InvalidOperationException(
            $"Submission {created.Id} did not reach a terminal state in time.");
    }

    private sealed record SubmissionCreatedDto(
        Guid Id,
        string Status,
        DateTimeOffset CreatedAtUtc);

    private sealed record SubmissionDetailDto(
        Guid Id,
        string Status,
        bool IsTerminal);

    private sealed record RubricCriterionDto(
        string Id,
        string Title,
        string? Description,
        int MaxPoints,
        int Weight,
        int Points);

    private sealed record RubricReviewDto(
        Guid SubmissionId,
        string ChallengeSlug,
        int Score,
        string Band,
        string? Note,
        List<RubricCriterionDto> Criteria,
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset UpdatedAtUtc);
}
