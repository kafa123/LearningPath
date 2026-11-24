using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Psel.Infrastructure.Persistence;
using Psel.Modules.Learning.Contracts;
using Psel.Modules.Learning.Domain;

namespace Psel.Api.Tests;

public sealed class LearningAuthoringEndpointTests
{
    [Fact]
    public async Task Import_AddsNewDomainAndModuleAndKeepsExistingRows()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        var pack = new
        {
            schemaVersion = 1,
            domains = new[]
            {
                new
                {
                    slug = "security-engineering",
                    name = "Security Engineering",
                    description = "Threat modeling and defensive coding.",
                    sortOrder = 6,
                    modules = new[]
                    {
                        new
                        {
                            slug = "threat-modeling",
                            title = "Threat Modeling",
                            summary = "Find the trust boundaries before writing code.",
                            level = "Beginner",
                            estimatedMinutes = 40,
                            sortOrder = 1,
                            objectives = new[]
                            {
                                new
                                {
                                    sortOrder = 1,
                                    text = "List assets, actors, and trust boundaries."
                                }
                            },
                            competencies = new[]
                            {
                                new
                                {
                                    slug = "threat-modeling",
                                    name = "Threat modeling",
                                    description = "Identify threats and choose a mitigation.",
                                    sortOrder = 1
                                }
                            },
                            materials = new[]
                            {
                                new
                                {
                                    slug = "threat-modeling-lesson",
                                    title = "Start with boundaries",
                                    kind = "Lesson",
                                    estimatedMinutes = 20,
                                    sortOrder = 1,
                                    bodyMarkdown = "## Trust boundaries\n\nList what crosses each boundary."
                                }
                            },
                            prerequisites = new[] { "algorithm-foundations" }
                        }
                    }
                }
            }
        };

        using var response = await client.PostAsJsonAsync(
            "/api/authoring/learning-content",
            pack,
            cancellationToken);

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<LearningContentImportResult>(
            cancellationToken);

        Assert.NotNull(result);
        Assert.Equal(1, result.DomainCount);
        Assert.Equal(1, result.ModuleCount);
        Assert.True(result.Added > 0);

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PselDbContext>();

        Assert.Equal(7, await dbContext.Set<LearningDomain>().CountAsync(cancellationToken));
        Assert.Equal(20, await dbContext.Set<LearningModule>().CountAsync(cancellationToken));
        Assert.Equal(20, await dbContext.Set<LearningMaterial>().CountAsync(cancellationToken));
        Assert.Equal(39, await dbContext.Set<Competency>().CountAsync(cancellationToken));
        Assert.Equal(58, await dbContext.Set<LearningObjective>().CountAsync(cancellationToken));
        Assert.Equal(
            14,
            await dbContext.Set<LearningModulePrerequisite>().CountAsync(cancellationToken));

        using var moduleResponse = await client.GetAsync(
            "/api/modules/threat-modeling",
            cancellationToken);
        moduleResponse.EnsureSuccessStatusCode();
        var moduleJson = await moduleResponse.Content.ReadAsStringAsync(cancellationToken);
        Assert.Contains("List assets, actors, and trust boundaries.", moduleJson);
        Assert.Contains("algorithm-foundations", moduleJson);
    }

    [Fact]
    public async Task Import_UpdatesExistingRowsInPlaceWithoutDuplicates()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        var pack = new
        {
            schemaVersion = 1,
            domains = new[]
            {
                new
                {
                    slug = "programming-fundamentals",
                    name = "Programming Fundamentals",
                    description = "Updated domain description.",
                    sortOrder = 1,
                    modules = new[]
                    {
                        new
                        {
                            slug = "complexity-analysis",
                            title = "Complexity Analysis Updated",
                            summary = "Updated module summary.",
                            level = "Intermediate",
                            estimatedMinutes = 55,
                            sortOrder = 2,
                            objectives = new[]
                            {
                                new
                                {
                                    sortOrder = 1,
                                    text = "Updated objective text."
                                }
                            },
                            competencies = new[]
                            {
                                new
                                {
                                    slug = "asymptotic-analysis",
                                    name = "Asymptotic analysis updated",
                                    description = "Updated competency description.",
                                    sortOrder = 1
                                }
                            },
                            materials = new[]
                            {
                                new
                                {
                                    slug = "complexity-analysis-lesson",
                                    title = "Updated lesson title",
                                    kind = "Lesson",
                                    estimatedMinutes = 25,
                                    sortOrder = 1,
                                    bodyMarkdown = "## Updated lesson body"
                                }
                            },
                            prerequisites = new[] { "algorithm-foundations" }
                        }
                    }
                }
            }
        };

        using var response = await client.PostAsJsonAsync(
            "/api/authoring/learning-content",
            pack,
            cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PselDbContext>();

        Assert.Equal(6, await dbContext.Set<LearningDomain>().CountAsync(cancellationToken));
        Assert.Equal(19, await dbContext.Set<LearningModule>().CountAsync(cancellationToken));
        Assert.Equal(19, await dbContext.Set<LearningMaterial>().CountAsync(cancellationToken));
        Assert.Equal(38, await dbContext.Set<Competency>().CountAsync(cancellationToken));
        Assert.Equal(57, await dbContext.Set<LearningObjective>().CountAsync(cancellationToken));
        Assert.Equal(
            13,
            await dbContext.Set<LearningModulePrerequisite>().CountAsync(cancellationToken));

        var module = await dbContext
            .Set<LearningModule>()
            .SingleAsync(candidate => candidate.Slug == "complexity-analysis", cancellationToken);
        Assert.Equal("Complexity Analysis Updated", module.Title);
        Assert.Equal(55, module.EstimatedMinutes);
        Assert.Equal(
            "Updated module summary.",
            module.Summary);

        var material = await dbContext
            .Set<LearningMaterial>()
            .SingleAsync(
                candidate => candidate.Slug == "complexity-analysis-lesson",
                cancellationToken);
        Assert.Equal("Updated lesson title", material.Title);
        Assert.Equal("## Updated lesson body", material.BodyMarkdown);

        var competency = await dbContext
            .Set<Competency>()
            .SingleAsync(
                candidate => candidate.Slug == "asymptotic-analysis",
                cancellationToken);
        Assert.Equal("Asymptotic analysis updated", competency.Name);

        var objective = await dbContext
            .Set<LearningObjective>()
            .SingleAsync(
                candidate => candidate.ModuleId == module.Id && candidate.SortOrder == 1,
                cancellationToken);
        Assert.Equal("Updated objective text.", objective.Text);
    }

    [Fact]
    public async Task Import_AcceptsMultipartJsonFile()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        var pack = new
        {
            schemaVersion = 1,
            domains = new[]
            {
                new
                {
                    slug = "quality-engineering",
                    name = "Quality Engineering",
                    description = "Testing and release confidence.",
                    sortOrder = 7,
                    modules = new[]
                    {
                        new
                        {
                            slug = "release-confidence",
                            title = "Release Confidence",
                            summary = "Decide what evidence a release needs.",
                            level = "Intermediate",
                            estimatedMinutes = 30,
                            sortOrder = 1,
                            objectives = new[]
                            {
                                new { sortOrder = 1, text = "Choose evidence for a release." }
                            },
                            competencies = Array.Empty<object>(),
                            materials = Array.Empty<object>(),
                            prerequisites = Array.Empty<string>()
                        }
                    }
                }
            }
        };

        var json = JsonSerializer.Serialize(pack);
        using var content = new MultipartFormDataContent();
        using var file = new ByteArrayContent(Encoding.UTF8.GetBytes(json));
        file.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        content.Add(file, "file", "learning.json");

        using var response = await client.PostAsync(
            "/api/authoring/learning-content",
            content,
            cancellationToken);

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<LearningContentImportResult>(
            cancellationToken);

        Assert.NotNull(result);
        Assert.Equal(1, result.DomainCount);
        Assert.Equal(1, result.ModuleCount);
    }

    [Fact]
    public async Task Import_RejectsUnknownPrerequisiteWithoutWritingRows()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        var pack = new
        {
            schemaVersion = 1,
            domains = new[]
            {
                new
                {
                    slug = "security-engineering",
                    name = "Security Engineering",
                    description = "Threat modeling.",
                    sortOrder = 6,
                    modules = new[]
                    {
                        new
                        {
                            slug = "threat-modeling",
                            title = "Threat Modeling",
                            summary = "Find trust boundaries.",
                            level = "Beginner",
                            estimatedMinutes = 40,
                            sortOrder = 1,
                            objectives = Array.Empty<object>(),
                            competencies = Array.Empty<object>(),
                            materials = Array.Empty<object>(),
                            prerequisites = new[] { "module-that-does-not-exist" }
                        }
                    }
                }
            }
        };

        using var response = await client.PostAsJsonAsync(
            "/api/authoring/learning-content",
            pack,
            cancellationToken);

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        Assert.Contains("module-that-does-not-exist", body);

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PselDbContext>();

        Assert.Equal(6, await dbContext.Set<LearningDomain>().CountAsync(cancellationToken));
        Assert.False(await dbContext
            .Set<LearningDomain>()
            .AnyAsync(domain => domain.Slug == "security-engineering", cancellationToken));
    }

    [Fact]
    public async Task Import_RejectsMovingExistingModuleToAnotherDomain()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        var pack = new
        {
            schemaVersion = 1,
            domains = new[]
            {
                new
                {
                    slug = "security-engineering",
                    name = "Security Engineering",
                    description = "Threat modeling.",
                    sortOrder = 6,
                    modules = new[]
                    {
                        new
                        {
                            slug = "complexity-analysis",
                            title = "Complexity Analysis",
                            summary = "Moved module attempt.",
                            level = "Beginner",
                            estimatedMinutes = 40,
                            sortOrder = 1,
                            objectives = Array.Empty<object>(),
                            competencies = Array.Empty<object>(),
                            materials = Array.Empty<object>(),
                            prerequisites = Array.Empty<string>()
                        }
                    }
                }
            }
        };

        using var response = await client.PostAsJsonAsync(
            "/api/authoring/learning-content",
            pack,
            cancellationToken);

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        Assert.Contains("cannot be moved by an import", body, StringComparison.Ordinal);

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PselDbContext>();

        Assert.False(await dbContext
            .Set<LearningDomain>()
            .AnyAsync(domain => domain.Slug == "security-engineering", cancellationToken));
        var module = await dbContext
            .Set<LearningModule>()
            .SingleAsync(candidate => candidate.Slug == "complexity-analysis", cancellationToken);
        Assert.Equal("Complexity Analysis", module.Title);
    }
}
