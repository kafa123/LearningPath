using System.Net;
using System.Net.Http.Json;

namespace Psel.Api.Tests;

public sealed class LearningEndpointTests
{
    [Fact]
    public async Task GetDomains_ReturnsFiveOrderedDomainsWithModuleCounts()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        var domains = await client.GetFromJsonAsync<List<DomainDto>>(
            "/api/domains",
            cancellationToken);

        Assert.NotNull(domains);
        Assert.Equal(5, domains.Count);
        Assert.Equal(
            [
                "programming-fundamentals",
                "database-engineering",
                "backend-engineering",
                "software-architecture",
                "software-quality"
            ],
            domains.Select(domain => domain.Slug));
        Assert.Equal(3, domains[0].ModuleCount);
        Assert.All(domains, domain => Assert.False(string.IsNullOrWhiteSpace(domain.Description)));
    }

    [Fact]
    public async Task GetLearningPaths_IncludesModulesAndPrerequisiteSlugs()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        var paths = await client.GetFromJsonAsync<List<LearningPathDto>>(
            "/api/learning-paths",
            cancellationToken);

        Assert.NotNull(paths);
        Assert.Equal(5, paths.Count);

        var programming = Assert.Single(
            paths,
            path => path.Slug == "programming-fundamentals");

        Assert.Equal(3, programming.Modules.Count);
        Assert.Equal(
            ["algorithm-foundations", "complexity-analysis", "data-structures"],
            programming.Modules.Select(module => module.Slug));

        var complexity = Assert.Single(
            programming.Modules,
            module => module.Slug == "complexity-analysis");

        Assert.Contains("algorithm-foundations", complexity.PrerequisiteSlugs);
        Assert.Equal(
            ["asymptotic-analysis", "performance-tradeoffs"],
            complexity.CompetencySlugs);
    }

    [Fact]
    public async Task GetLearningPath_ReturnsDomainDetail()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        var path = await client.GetFromJsonAsync<LearningPathDto>(
            "/api/learning-paths/database-engineering",
            cancellationToken);

        Assert.NotNull(path);
        Assert.Equal("Database Engineering", path.Name);
        Assert.Equal(3, path.Modules.Count);
    }

    [Fact]
    public async Task GetLearningPath_UnknownSlug_ReturnsNotFoundProblem()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(
            "/api/learning-paths/does-not-exist",
            cancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task GetModule_ReturnsObjectivesMaterialsCompetenciesAndPrerequisites()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        var module = await client.GetFromJsonAsync<ModuleDto>(
            "/api/modules/complexity-analysis",
            cancellationToken);

        Assert.NotNull(module);
        Assert.Equal("Complexity Analysis", module.Title);
        Assert.Equal("Programming Fundamentals", module.Domain.Name);
        Assert.Equal(3, module.Objectives.Count);
        Assert.Contains(
            module.Objectives,
            objective => objective.Contains("dominant operation", StringComparison.Ordinal));
        Assert.Single(module.Materials);
        Assert.Equal("complexity-analysis-lesson", module.Materials[0].Slug);
        Assert.Equal(2, module.Competencies.Count);
        Assert.Contains(
            module.Prerequisites,
            prerequisite => prerequisite.Slug == "algorithm-foundations");
    }

    [Fact]
    public async Task GetModule_UnknownSlug_ReturnsNotFound()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(
            "/api/modules/does-not-exist",
            cancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetMaterial_ReturnsMarkdownBodyAndModuleContext()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        var material = await client.GetFromJsonAsync<MaterialDto>(
            "/api/materials/algorithm-foundations-lesson",
            cancellationToken);

        Assert.NotNull(material);
        Assert.Equal("From problem to algorithm", material.Title);
        Assert.Equal("algorithm-foundations", material.Module.Slug);
        Assert.Equal("programming-fundamentals", material.Module.DomainSlug);
        Assert.Contains("## From problem to algorithm", material.BodyMarkdown);
    }

    [Fact]
    public async Task GetMaterial_UnknownSlug_ReturnsNotFound()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(
            "/api/materials/does-not-exist",
            cancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private sealed record DomainDto(
        string Slug,
        string Name,
        string Description,
        int ModuleCount);

    private sealed record LearningPathDto(
        string Slug,
        string Name,
        string Description,
        List<ModuleSummaryDto> Modules);

    private sealed record ModuleSummaryDto(
        string Slug,
        string Title,
        string Summary,
        string Level,
        int EstimatedMinutes,
        List<string> PrerequisiteSlugs,
        List<string> CompetencySlugs);

    private sealed record DomainReferenceDto(string Slug, string Name);

    private sealed record ModuleReferenceDto(string Slug, string Title, string DomainSlug);

    private sealed record MaterialSummaryDto(
        string Slug,
        string Title,
        string Kind,
        int EstimatedMinutes);

    private sealed record CompetencyDto(string Slug, string Name, string Description);

    private sealed record ModuleDto(
        string Slug,
        string Title,
        string Summary,
        string Level,
        int EstimatedMinutes,
        DomainReferenceDto Domain,
        List<string> Objectives,
        List<MaterialSummaryDto> Materials,
        List<CompetencyDto> Competencies,
        List<ModuleReferenceDto> Prerequisites);

    private sealed record MaterialDto(
        string Slug,
        string Title,
        string Kind,
        int EstimatedMinutes,
        ModuleReferenceDto Module,
        string BodyMarkdown);
}
