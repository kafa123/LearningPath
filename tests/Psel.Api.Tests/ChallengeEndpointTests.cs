using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Psel.Api.Tests;

public sealed class ChallengeEndpointTests
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task GetChallenges_ReturnsValidatedChallengesWithoutHiddenMaterial()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/challenges", cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        var challenges = JsonSerializer.Deserialize<List<ChallengeSummaryDto>>(body, JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(challenges);
        Assert.Equal(
            [
                "api-validation",
                "booking-constraint-audit",
                "data-integrity-audit",
                "balanced-brackets",
                "tenant-activity-feed",
                "order-revenue",
                "debug-off-by-one",
                "debug-cart-totals",
                "extract-checkout-port",
                "inject-pricing-clock",
                "error-response-mapping",
                "refactor-duplication",
                "two-sum",
                "order-index-plan",
                "page-request-contract",
                "word-frequency"
            ],
            challenges.Select(challenge => challenge.Slug));

        Assert.Equal(
            [
                "Add Request Validation to an Endpoint",
                "Audit booking constraints",
                "Audit relational data integrity",
                "Balanced Brackets",
                "Build a tenant activity feed",
                "Completed revenue per customer",
                "Debug an Off-by-One Sum",
                "Debug cart totals and coupons",
                "Extract a checkout notifier port",
                "Inject a clock into pricing",
                "Map Domain Errors to HTTP Responses",
                "Refactor Duplicated Discount Rules",
                "Two Sum",
                "Use the composite order index",
                "Validate a Page Request Contract",
                "Word Frequency"
            ],
            challenges.Select(challenge => challenge.Title));

        Assert.All(
            challenges,
            challenge =>
            {
                Assert.False(string.IsNullOrWhiteSpace(challenge.Summary));
                Assert.False(string.IsNullOrWhiteSpace(challenge.DomainSlug));
                Assert.False(string.IsNullOrWhiteSpace(challenge.Type));
                Assert.InRange(challenge.EstimatedMinutes, 5, 600);
                Assert.NotEmpty(challenge.Languages);
                Assert.All(
                    challenge.Languages,
                    language =>
                    {
                        Assert.False(string.IsNullOrWhiteSpace(language.Id));
                        Assert.False(string.IsNullOrWhiteSpace(language.Name));
                    });
            });

        var twoSum = Assert.Single(
            challenges,
            challenge => challenge.Slug == "two-sum");
        Assert.Equal(["csharp", "python"], twoSum.Languages.Select(language => language.Id));
        Assert.All(twoSum.Languages, language => Assert.True(language.CanExecute));

        var apiValidation = Assert.Single(
            challenges,
            challenge => challenge.Slug == "api-validation");
        Assert.All(apiValidation.Languages, language => Assert.False(language.CanExecute));

        Assert.DoesNotContain("tests/hidden", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("solutions/", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("NotImplementedException", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetChallenge_ReturnsStatementStarterFilesPublicTestsAndEvaluation()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/challenges/two-sum", cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        var challenge = JsonSerializer.Deserialize<ChallengeDetailDto>(body, JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(challenge);
        Assert.Equal("Two Sum", challenge.Title);
        Assert.Equal("programming-fundamentals", challenge.DomainSlug);
        Assert.Equal("Coding", challenge.Type);
        Assert.Contains("# Two Sum", challenge.StatementMarkdown, StringComparison.Ordinal);
        Assert.False(string.IsNullOrWhiteSpace(challenge.HintsMarkdown));

        Assert.Equal(3, challenge.Objectives.Count);
        Assert.Equal(3, challenge.Constraints.Count);
        Assert.Equal(2, challenge.Examples.Count);
        Assert.Equal(
            ["algorithm-foundations", "data-structures"],
            challenge.PrerequisiteSlugs);

        var csharp = Assert.Single(
            challenge.Languages,
            language => language.Id == "csharp");
        Assert.Equal("C#", csharp.Name);
        Assert.Equal("csharp", csharp.EditorLanguage);
        var starter = Assert.Single(csharp.StarterFiles);
        Assert.Equal("Solution.cs", starter.Path);
        Assert.Contains("NotImplementedException", starter.Content, StringComparison.Ordinal);

        var python = Assert.Single(
            challenge.Languages,
            language => language.Id == "python");
        Assert.Equal("python", python.EditorLanguage);
        Assert.True(python.CanExecute);
        Assert.Contains(
            "NotImplementedError",
            python.StarterFiles[0].Content,
            StringComparison.Ordinal);

        Assert.Equal(2, challenge.PublicTests.Count);
        Assert.Equal(
            ["Example 1", "Example 2"],
            challenge.PublicTests.Select(test => test.Name));
        Assert.All(
            challenge.PublicTests,
            test => Assert.False(string.IsNullOrWhiteSpace(test.Description)));

        Assert.Equal("AutomatedTests", challenge.Evaluation.Kind);
        Assert.Equal(5, challenge.Evaluation.TimeLimitSeconds);
        Assert.Equal(256, challenge.Evaluation.MemoryLimitMb);
        Assert.Null(challenge.Rubric);

        Assert.DoesNotContain("tests/hidden", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("solutions/", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"hidden\"", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("negative-numbers", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetChallenge_EveryPackageServesADetail()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        string[] slugs =
        [
            "api-validation",
            "booking-constraint-audit",
            "data-integrity-audit",
            "balanced-brackets",
            "tenant-activity-feed",
            "order-revenue",
            "debug-off-by-one",
            "debug-cart-totals",
            "extract-checkout-port",
            "inject-pricing-clock",
            "error-response-mapping",
            "refactor-duplication",
            "two-sum",
            "order-index-plan",
            "page-request-contract",
            "word-frequency"
        ];

        foreach (var slug in slugs)
        {
            using var response = await client.GetAsync(
                $"/api/challenges/{slug}",
                cancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var challenge = await response.Content.ReadFromJsonAsync<ChallengeDetailDto>(
                JsonOptions,
                cancellationToken);

            Assert.NotNull(challenge);
            Assert.Equal(slug, challenge.Slug);
            Assert.NotEmpty(challenge.Languages);
            Assert.NotEmpty(challenge.PublicTests);
        }
    }

    [Fact]
    public async Task GetChallenge_ReturnsServerOwnedRubricWhenDefined()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(
            "/api/challenges/refactor-duplication",
            cancellationToken);
        var challenge = await response.Content.ReadFromJsonAsync<ChallengeDetailDto>(
            JsonOptions,
            cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(challenge);
        Assert.NotNull(challenge.Rubric);
        Assert.Equal("Hybrid", challenge.Evaluation.Kind);
        Assert.Equal(
            ["behavior-preservation", "single-source-of-truth", "clarity", "test-confidence"],
            challenge.Rubric.Criteria.Select(criterion => criterion.Id));
        Assert.All(
            challenge.Rubric.Criteria,
            criterion =>
            {
                Assert.False(string.IsNullOrWhiteSpace(criterion.Title));
                Assert.False(string.IsNullOrWhiteSpace(criterion.Description));
                Assert.Equal(10, criterion.MaxPoints);
                Assert.InRange(criterion.Weight, 1, 10);
            });

        var behavior = challenge.Rubric.Criteria[0];
        Assert.Equal(3, behavior.Weight);
        Assert.Equal("Behavior preservation", behavior.Title);
    }

    [Fact]
    public async Task GetChallenge_UnknownSlug_ReturnsNotFoundProblem()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(
            "/api/challenges/does-not-exist",
            cancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        Assert.Contains("Challenge not found.", body, StringComparison.Ordinal);
    }

    private sealed record ChallengeLanguageSummaryDto(string Id, string Name, bool CanExecute);

    private sealed record ChallengeSummaryDto(
        string Slug,
        string Title,
        string Summary,
        string DomainSlug,
        string Topic,
        string Difficulty,
        string Type,
        int EstimatedMinutes,
        List<ChallengeLanguageSummaryDto> Languages,
        List<string> PrerequisiteSlugs);

    private sealed record ChallengeFileDto(string Path, string Content);

    private sealed record ChallengeLanguageDto(
        string Id,
        string Name,
        string EditorLanguage,
        List<ChallengeFileDto> StarterFiles,
        bool CanExecute);

    private sealed record ChallengeExampleDto(
        string Input,
        string Output,
        string? Explanation);

    private sealed record PublicTestSummaryDto(string Name, string? Description);

    private sealed record ChallengeEvaluationDto(
        string Kind,
        int? TimeLimitSeconds,
        int? MemoryLimitMb);

    private sealed record RubricCriterionDto(
        string Id,
        string Title,
        string? Description,
        int MaxPoints,
        int Weight);

    private sealed record ChallengeRubricDto(List<RubricCriterionDto> Criteria);

    private sealed record ChallengeDetailDto(
        string Slug,
        string Title,
        string Summary,
        string DomainSlug,
        string Topic,
        string Difficulty,
        string Type,
        int EstimatedMinutes,
        List<string> Objectives,
        List<string> PrerequisiteSlugs,
        List<string> Constraints,
        List<ChallengeExampleDto> Examples,
        List<ChallengeLanguageDto> Languages,
        List<PublicTestSummaryDto> PublicTests,
        ChallengeEvaluationDto Evaluation,
        ChallengeRubricDto? Rubric,
        string StatementMarkdown,
        string? HintsMarkdown);
}
