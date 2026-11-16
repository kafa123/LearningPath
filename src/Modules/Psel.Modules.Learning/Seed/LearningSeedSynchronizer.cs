using Microsoft.EntityFrameworkCore;
using Psel.Infrastructure.Persistence;
using Psel.Modules.Learning.Domain;

namespace Psel.Modules.Learning.Seed;

internal sealed record LearningSeedSyncResult(int Added, int Updated)
{
    public int Total => Added + Updated;
}

/// <summary>
/// Applies the seed catalog as an upsert. Existing rows are matched by slug,
/// and objectives by module and sort order. Restarting the API can therefore
/// add or refresh learning content without deleting submissions, assessments,
/// or rubric reviews. Rows removed from the seed are deliberately left in
/// place; destructive cleanup still requires a database reset.
/// </summary>
internal static class LearningSeedSynchronizer
{
    public static async Task<LearningSeedSyncResult> SynchronizeAsync(
        PselDbContext dbContext,
        IReadOnlyList<LearningDomain> seedDomains,
        CancellationToken cancellationToken)
    {
        var added = 0;
        var updated = 0;

        var domains = (await dbContext.Set<LearningDomain>().ToListAsync(cancellationToken))
            .ToDictionary(domain => domain.Slug, StringComparer.Ordinal);
        var modules = (await dbContext.Set<LearningModule>().ToListAsync(cancellationToken))
            .ToDictionary(module => module.Slug, StringComparer.Ordinal);
        var materials = (await dbContext.Set<LearningMaterial>().ToListAsync(cancellationToken))
            .ToDictionary(material => material.Slug, StringComparer.Ordinal);
        var competencies = (await dbContext.Set<Competency>().ToListAsync(cancellationToken))
            .ToDictionary(competency => competency.Slug, StringComparer.Ordinal);
        var objectives = (await dbContext.Set<LearningObjective>().ToListAsync(cancellationToken))
            .ToDictionary(objective => (objective.ModuleId, objective.SortOrder));
        var prerequisites = (await dbContext
                .Set<LearningModulePrerequisite>()
                .ToListAsync(cancellationToken))
            .Select(prerequisite => (prerequisite.ModuleId, prerequisite.PrerequisiteModuleId))
            .ToHashSet();

        foreach (var seedDomain in seedDomains)
        {
            var isNewDomain = !domains.TryGetValue(seedDomain.Slug, out var domain);

            if (isNewDomain)
            {
                domain = new LearningDomain { Slug = seedDomain.Slug };
                dbContext.Add(domain);
                domains[seedDomain.Slug] = domain;
            }

            var domainChanged = false;
            domainChanged |= Apply(domain!.Name, seedDomain.Name, value => domain.Name = value);
            domainChanged |= Apply(
                domain.Description,
                seedDomain.Description,
                value => domain.Description = value);
            domainChanged |= Apply(
                domain.SortOrder,
                seedDomain.SortOrder,
                value => domain.SortOrder = value);

            if (isNewDomain)
            {
                added++;
            }
            else if (domainChanged)
            {
                updated++;
            }

            foreach (var seedModule in seedDomain.Modules)
            {
                var isNewModule = !modules.TryGetValue(seedModule.Slug, out var module);

                if (isNewModule)
                {
                    module = new LearningModule { Slug = seedModule.Slug };
                    dbContext.Add(module);
                    modules[seedModule.Slug] = module;
                }

                var moduleChanged = false;
                moduleChanged |= Apply(
                    module!.DomainId,
                    domain.Id,
                    value => module.DomainId = value);
                module.Domain = domain;
                moduleChanged |= Apply(module.Title, seedModule.Title, value => module.Title = value);
                moduleChanged |= Apply(
                    module.Summary,
                    seedModule.Summary,
                    value => module.Summary = value);
                moduleChanged |= Apply(module.Level, seedModule.Level, value => module.Level = value);
                moduleChanged |= Apply(
                    module.EstimatedMinutes,
                    seedModule.EstimatedMinutes,
                    value => module.EstimatedMinutes = value);
                moduleChanged |= Apply(
                    module.SortOrder,
                    seedModule.SortOrder,
                    value => module.SortOrder = value);

                if (isNewModule)
                {
                    added++;
                }
                else if (moduleChanged)
                {
                    updated++;
                }

                foreach (var seedObjective in seedModule.Objectives)
                {
                    var key = (module.Id, seedObjective.SortOrder);
                    var isNewObjective = !objectives.TryGetValue(key, out var objective);

                    if (isNewObjective)
                    {
                        objective = new LearningObjective
                        {
                            ModuleId = module.Id,
                            SortOrder = seedObjective.SortOrder
                        };
                        dbContext.Add(objective);
                        objectives[key] = objective;
                    }

                    var objectiveChanged = Apply(
                        objective!.Text,
                        seedObjective.Text,
                        value => objective.Text = value);

                    if (isNewObjective)
                    {
                        added++;
                    }
                    else if (objectiveChanged)
                    {
                        updated++;
                    }
                }

                foreach (var seedMaterial in seedModule.Materials)
                {
                    var isNewMaterial = !materials.TryGetValue(seedMaterial.Slug, out var material);

                    if (isNewMaterial)
                    {
                        material = new LearningMaterial { Slug = seedMaterial.Slug };
                        dbContext.Add(material);
                        materials[seedMaterial.Slug] = material;
                    }

                    var materialChanged = false;
                    materialChanged |= Apply(
                        material!.ModuleId,
                        module.Id,
                        value => material.ModuleId = value);
                    material.Module = module;
                    materialChanged |= Apply(
                        material.Title,
                        seedMaterial.Title,
                        value => material.Title = value);
                    materialChanged |= Apply(
                        material.Kind,
                        seedMaterial.Kind,
                        value => material.Kind = value);
                    materialChanged |= Apply(
                        material.EstimatedMinutes,
                        seedMaterial.EstimatedMinutes,
                        value => material.EstimatedMinutes = value);
                    materialChanged |= Apply(
                        material.SortOrder,
                        seedMaterial.SortOrder,
                        value => material.SortOrder = value);
                    materialChanged |= Apply(
                        material.BodyMarkdown,
                        seedMaterial.BodyMarkdown,
                        value => material.BodyMarkdown = value);

                    if (isNewMaterial)
                    {
                        added++;
                    }
                    else if (materialChanged)
                    {
                        updated++;
                    }
                }

                foreach (var seedCompetency in seedModule.Competencies)
                {
                    var isNewCompetency = !competencies.TryGetValue(
                        seedCompetency.Slug,
                        out var competency);

                    if (isNewCompetency)
                    {
                        competency = new Competency { Slug = seedCompetency.Slug };
                        dbContext.Add(competency);
                        competencies[seedCompetency.Slug] = competency;
                    }

                    var competencyChanged = false;
                    competencyChanged |= Apply(
                        competency!.ModuleId,
                        module.Id,
                        value => competency.ModuleId = value);
                    competency.Module = module;
                    competencyChanged |= Apply(
                        competency.Name,
                        seedCompetency.Name,
                        value => competency.Name = value);
                    competencyChanged |= Apply(
                        competency.Description,
                        seedCompetency.Description,
                        value => competency.Description = value);
                    competencyChanged |= Apply(
                        competency.SortOrder,
                        seedCompetency.SortOrder,
                        value => competency.SortOrder = value);

                    if (isNewCompetency)
                    {
                        added++;
                    }
                    else if (competencyChanged)
                    {
                        updated++;
                    }
                }
            }
        }

        foreach (var seedModule in seedDomains.SelectMany(domain => domain.Modules))
        {
            var module = modules[seedModule.Slug];

            foreach (var seedPrerequisite in seedModule.Prerequisites)
            {
                var prerequisiteSlug = seedPrerequisite.PrerequisiteModule?.Slug;

                if (prerequisiteSlug is null
                    || !modules.TryGetValue(prerequisiteSlug, out var prerequisite))
                {
                    throw new InvalidOperationException(
                        $"Learning seed module '{seedModule.Slug}' references unknown prerequisite "
                        + $"'{prerequisiteSlug ?? "<null>"}'.");
                }

                if (!prerequisites.Add((module.Id, prerequisite.Id)))
                {
                    continue;
                }

                dbContext.Add(new LearningModulePrerequisite
                {
                    ModuleId = module.Id,
                    PrerequisiteModuleId = prerequisite.Id
                });
                added++;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return new LearningSeedSyncResult(added, updated);
    }

    private static bool Apply<T>(T current, T next, Action<T> assign)
    {
        if (EqualityComparer<T>.Default.Equals(current, next))
        {
            return false;
        }

        assign(next);

        return true;
    }
}
