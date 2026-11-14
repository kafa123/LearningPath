using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Psel.Infrastructure.Persistence;
using Psel.Modules.Learning.Contracts;
using Psel.Modules.Learning.Domain;

namespace Psel.Modules.Learning.Endpoints;

internal static class LearningEndpoints
{
    public static IEndpointRouteBuilder MapLearningEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api").WithTags("Learning");

        group.MapGet("/domains", GetDomains)
            .WithName("ListLearningDomains")
            .WithSummary("List the available learning domains.");

        group.MapGet("/learning-paths", GetLearningPaths)
            .WithName("ListLearningPaths")
            .WithSummary("List learning paths with their modules.");

        group.MapGet("/learning-paths/{slug}", GetLearningPath)
            .WithName("GetLearningPath")
            .WithSummary("Get one learning path and its module summaries.");

        group.MapGet("/modules/{slug}", GetModule)
            .WithName("GetLearningModule")
            .WithSummary("Get a module with objectives, materials, and competencies.");

        group.MapGet("/materials/{slug}", GetMaterial)
            .WithName("GetLearningMaterial")
            .WithSummary("Get one learning material, including its Markdown body.");

        return endpoints;
    }

    private static async Task<Ok<IReadOnlyList<DomainSummary>>> GetDomains(
        PselDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var domains = await dbContext.Set<LearningDomain>()
            .AsNoTracking()
            .OrderBy(domain => domain.SortOrder)
            .Select(domain => new DomainSummary(
                domain.Slug,
                domain.Name,
                domain.Description,
                domain.Modules.Count))
            .ToListAsync(cancellationToken);

        return TypedResults.Ok<IReadOnlyList<DomainSummary>>(domains);
    }

    private static async Task<Ok<IReadOnlyList<LearningPathSummary>>> GetLearningPaths(
        PselDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var paths = await dbContext.Set<LearningDomain>()
            .AsNoTracking()
            .AsSplitQuery()
            .OrderBy(domain => domain.SortOrder)
            .Select(domain => new LearningPathSummary(
                domain.Slug,
                domain.Name,
                domain.Description,
                domain.Modules
                    .OrderBy(module => module.SortOrder)
                    .Select(module => new ModuleSummary(
                        module.Slug,
                        module.Title,
                        module.Summary,
                        module.Level,
                        module.EstimatedMinutes,
                        module.Prerequisites
                            .Select(prerequisite => prerequisite.PrerequisiteModule!.Slug)
                            .OrderBy(slug => slug)
                            .ToList()))
                    .ToList()))
            .ToListAsync(cancellationToken);

        return TypedResults.Ok<IReadOnlyList<LearningPathSummary>>(paths);
    }

    private static async Task<Results<Ok<LearningPathSummary>, ProblemHttpResult>> GetLearningPath(
        string slug,
        PselDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var path = await dbContext.Set<LearningDomain>()
            .AsNoTracking()
            .AsSplitQuery()
            .Where(domain => domain.Slug == slug)
            .Select(domain => new LearningPathSummary(
                domain.Slug,
                domain.Name,
                domain.Description,
                domain.Modules
                    .OrderBy(module => module.SortOrder)
                    .Select(module => new ModuleSummary(
                        module.Slug,
                        module.Title,
                        module.Summary,
                        module.Level,
                        module.EstimatedMinutes,
                        module.Prerequisites
                            .Select(prerequisite => prerequisite.PrerequisiteModule!.Slug)
                            .OrderBy(prerequisiteSlug => prerequisiteSlug)
                            .ToList()))
                    .ToList()))
            .SingleOrDefaultAsync(cancellationToken);

        return path is null
            ? TypedResults.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Learning path not found.",
                detail: "No learning path matches the requested slug.")
            : TypedResults.Ok(path);
    }

    private static async Task<Results<Ok<ModuleDetail>, ProblemHttpResult>> GetModule(
        string slug,
        PselDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var module = await dbContext.Set<LearningModule>()
            .AsNoTracking()
            .AsSplitQuery()
            .Where(candidate => candidate.Slug == slug)
            .Select(candidate => new ModuleDetail(
                candidate.Slug,
                candidate.Title,
                candidate.Summary,
                candidate.Level,
                candidate.EstimatedMinutes,
                new DomainReference(candidate.Domain!.Slug, candidate.Domain.Name),
                candidate.Objectives
                    .OrderBy(objective => objective.SortOrder)
                    .Select(objective => objective.Text)
                    .ToList(),
                candidate.Materials
                    .OrderBy(material => material.SortOrder)
                    .Select(material => new MaterialSummary(
                        material.Slug,
                        material.Title,
                        material.Kind,
                        material.EstimatedMinutes))
                    .ToList(),
                candidate.Competencies
                    .OrderBy(competency => competency.SortOrder)
                    .Select(competency => new CompetencySummary(
                        competency.Slug,
                        competency.Name,
                        competency.Description))
                    .ToList(),
                candidate.Prerequisites
                    .Select(prerequisite => new ModuleReference(
                        prerequisite.PrerequisiteModule!.Slug,
                        prerequisite.PrerequisiteModule.Title,
                        prerequisite.PrerequisiteModule.Domain!.Slug))
                    .ToList()))
            .SingleOrDefaultAsync(cancellationToken);

        return module is null
            ? TypedResults.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Learning module not found.",
                detail: "No learning module matches the requested slug.")
            : TypedResults.Ok(module);
    }

    private static async Task<Results<Ok<MaterialDetail>, ProblemHttpResult>> GetMaterial(
        string slug,
        PselDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var material = await dbContext.Set<LearningMaterial>()
            .AsNoTracking()
            .Where(candidate => candidate.Slug == slug)
            .Select(candidate => new MaterialDetail(
                candidate.Slug,
                candidate.Title,
                candidate.Kind,
                candidate.EstimatedMinutes,
                new ModuleReference(
                    candidate.Module!.Slug,
                    candidate.Module.Title,
                    candidate.Module.Domain!.Slug),
                candidate.BodyMarkdown))
            .SingleOrDefaultAsync(cancellationToken);

        return material is null
            ? TypedResults.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Learning material not found.",
                detail: "No learning material matches the requested slug.")
            : TypedResults.Ok(material);
    }
}
