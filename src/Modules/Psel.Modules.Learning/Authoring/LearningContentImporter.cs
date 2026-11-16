using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Psel.Infrastructure.Persistence;
using Psel.Modules.Learning.Contracts;
using Psel.Modules.Learning.Domain;
using Psel.Modules.Learning.Seed;

namespace Psel.Modules.Learning.Authoring;

internal sealed class LearningContentImportException(IReadOnlyList<string> errors)
    : Exception(
        "Learning content import failed: "
        + string.Join(" | ", errors))
{
    public IReadOnlyList<string> Errors { get; } = errors;
}

/// <summary>
/// Validates a JSON content pack and applies it through the same upsert path as
/// the built-in seed catalog. Existing rows and learner evidence are preserved.
/// </summary>
internal sealed class LearningContentImporter(PselDbContext dbContext)
{
    private const int MaxDomains = 50;
    private const int MaxModulesPerDomain = 100;
    private const int MaxObjectives = 50;
    private const int MaxCompetencies = 50;
    private const int MaxMaterials = 50;
    private const int MaxPrerequisites = 20;
    private const int MaxMaterialBodyLength = 200_000;

    private static readonly Regex SlugPattern = new(
        "^[a-z0-9]+(?:-[a-z0-9]+)*$",
        RegexOptions.CultureInvariant);

    private static readonly HashSet<string> Levels =
    [
        "Beginner",
        "Intermediate",
        "Advanced"
    ];

    public async Task<LearningContentImportResult> ImportAsync(
        LearningContentPack pack,
        CancellationToken cancellationToken)
    {
        var errors = new List<string>();
        var domainImports = pack.Domains ?? [];

        ValidatePackShape(pack, domainImports, errors);

        var existingModules = await dbContext
            .Set<LearningModule>()
            .AsNoTracking()
            .Select(module => new
            {
                module.Slug,
                DomainSlug = module.Domain!.Slug
            })
            .ToListAsync(cancellationToken);
        var existingMaterials = await dbContext
            .Set<LearningMaterial>()
            .AsNoTracking()
            .Select(material => new { material.Slug, ModuleSlug = material.Module!.Slug })
            .ToListAsync(cancellationToken);
        var existingCompetencies = await dbContext
            .Set<Competency>()
            .AsNoTracking()
            .Select(competency => new { competency.Slug, ModuleSlug = competency.Module!.Slug })
            .ToListAsync(cancellationToken);

        var modulesBySlug = existingModules.ToDictionary(
            module => module.Slug,
            module => module.DomainSlug,
            StringComparer.Ordinal);
        var materialsBySlug = existingMaterials.ToDictionary(
            material => material.Slug,
            material => material.ModuleSlug,
            StringComparer.Ordinal);
        var competenciesBySlug = existingCompetencies.ToDictionary(
            competency => competency.Slug,
            competency => competency.ModuleSlug,
            StringComparer.Ordinal);

        var domainSlugs = new HashSet<string>(StringComparer.Ordinal);
        var moduleSlugs = new HashSet<string>(StringComparer.Ordinal);
        var materialSlugs = new HashSet<string>(StringComparer.Ordinal);
        var competencySlugs = new HashSet<string>(StringComparer.Ordinal);
        var importedModules = new List<(LearningModuleImport Module, string Path)>();

        for (var domainIndex = 0; domainIndex < domainImports.Count; domainIndex++)
        {
            var domain = domainImports[domainIndex];
            var domainPath = $"domains[{domainIndex}]";
            var domainSlug = ValidateSlug(
                domain.Slug,
                $"{domainPath}.slug",
                100,
                errors);

            if (domainSlug is not null && !domainSlugs.Add(domainSlug))
            {
                errors.Add($"{domainPath}.slug '{domainSlug}' is duplicated in the pack.");
            }

            ValidateRequiredText(domain.Name, $"{domainPath}.name", 150, errors);
            ValidateRequiredText(
                domain.Description,
                $"{domainPath}.description",
                1000,
                errors);
            ValidateSortOrder(domain.SortOrder, $"{domainPath}.sortOrder", errors);

            var modules = domain.Modules ?? [];

            if (modules.Count == 0)
            {
                errors.Add($"{domainPath}.modules must contain at least one module.");
            }

            if (modules.Count > MaxModulesPerDomain)
            {
                errors.Add(
                    $"{domainPath}.modules must contain at most {MaxModulesPerDomain} entries.");
            }

            for (var moduleIndex = 0; moduleIndex < modules.Count; moduleIndex++)
            {
                var module = modules[moduleIndex];
                var modulePath = $"{domainPath}.modules[{moduleIndex}]";
                var moduleSlug = ValidateSlug(
                    module.Slug,
                    $"{modulePath}.slug",
                    120,
                    errors);

                ValidateRequiredText(module.Title, $"{modulePath}.title", 150, errors);
                ValidateRequiredText(module.Summary, $"{modulePath}.summary", 600, errors);
                ValidateLevel(module.Level, $"{modulePath}.level", errors);
                ValidateEstimatedMinutes(
                    module.EstimatedMinutes,
                    $"{modulePath}.estimatedMinutes",
                    errors);
                ValidateSortOrder(module.SortOrder, $"{modulePath}.sortOrder", errors);

                if (moduleSlug is not null)
                {
                    if (!moduleSlugs.Add(moduleSlug))
                    {
                        errors.Add($"{modulePath}.slug '{moduleSlug}' is duplicated in the pack.");
                    }

                    if (domainSlug is not null
                        && modulesBySlug.TryGetValue(moduleSlug, out var existingDomain)
                        && !string.Equals(existingDomain, domainSlug, StringComparison.Ordinal))
                    {
                        errors.Add(
                            $"{modulePath}.slug '{moduleSlug}' already belongs to domain "
                            + $"'{existingDomain}' and cannot be moved by an import.");
                    }

                    importedModules.Add((module, modulePath));
                }

                ValidateObjectives(module, modulePath, errors);
                ValidateCompetencies(
                    module,
                    modulePath,
                    competenciesBySlug,
                    competencySlugs,
                    errors);
                ValidateMaterials(
                    module,
                    modulePath,
                    materialsBySlug,
                    materialSlugs,
                    errors);
                ValidatePrerequisites(module, modulePath, moduleSlug, errors);
            }
        }

        foreach (var (module, modulePath) in importedModules)
        {
            foreach (var prerequisiteSlug in module.Prerequisites ?? [])
            {
                if (string.IsNullOrWhiteSpace(prerequisiteSlug))
                {
                    continue;
                }

                if (!moduleSlugs.Contains(prerequisiteSlug)
                    && !modulesBySlug.ContainsKey(prerequisiteSlug))
                {
                    errors.Add(
                        $"{modulePath}.prerequisites references unknown module "
                        + $"'{prerequisiteSlug}'.");
                }
            }
        }

        if (errors.Count > 0)
        {
            throw new LearningContentImportException(errors);
        }

        var seedDomains = BuildSeedDomains(domainImports);
        var syncResult = await LearningSeedSynchronizer.SynchronizeAsync(
            dbContext,
            seedDomains,
            cancellationToken);

        return new LearningContentImportResult(
            syncResult.Added,
            syncResult.Updated,
            domainImports.Count,
            importedModules.Count);
    }

    private static void ValidatePackShape(
        LearningContentPack pack,
        IReadOnlyList<LearningDomainImport> domains,
        List<string> errors)
    {
        if (pack.SchemaVersion != 1)
        {
            errors.Add($"schemaVersion must be 1; found {pack.SchemaVersion}.");
        }

        if (domains.Count == 0)
        {
            errors.Add("domains must contain at least one domain.");
        }

        if (domains.Count > MaxDomains)
        {
            errors.Add($"domains must contain at most {MaxDomains} entries.");
        }
    }

    private static void ValidateObjectives(
        LearningModuleImport module,
        string modulePath,
        List<string> errors)
    {
        var objectives = module.Objectives ?? [];

        if (objectives.Count > MaxObjectives)
        {
            errors.Add(
                $"{modulePath}.objectives must contain at most {MaxObjectives} entries.");
        }

        var sortOrders = new HashSet<int>();

        for (var index = 0; index < objectives.Count; index++)
        {
            var objective = objectives[index];
            var path = $"{modulePath}.objectives[{index}]";

            ValidateSortOrder(objective.SortOrder, $"{path}.sortOrder", errors);

            if (!sortOrders.Add(objective.SortOrder))
            {
                errors.Add($"{path}.sortOrder {objective.SortOrder} is duplicated.");
            }

            ValidateRequiredText(objective.Text, $"{path}.text", 400, errors);
        }
    }

    private static void ValidateCompetencies(
        LearningModuleImport module,
        string modulePath,
        IReadOnlyDictionary<string, string> existingModules,
        HashSet<string> packSlugs,
        List<string> errors)
    {
        var competencies = module.Competencies ?? [];
        var moduleSlug = module.Slug ?? string.Empty;

        if (competencies.Count > MaxCompetencies)
        {
            errors.Add(
                $"{modulePath}.competencies must contain at most {MaxCompetencies} entries.");
        }

        var sortOrders = new HashSet<int>();

        for (var index = 0; index < competencies.Count; index++)
        {
            var competency = competencies[index];
            var path = $"{modulePath}.competencies[{index}]";
            var slug = ValidateSlug(competency.Slug, $"{path}.slug", 140, errors);

            ValidateRequiredText(competency.Name, $"{path}.name", 150, errors);
            ValidateRequiredText(competency.Description, $"{path}.description", 800, errors);
            ValidateSortOrder(competency.SortOrder, $"{path}.sortOrder", errors);

            if (!sortOrders.Add(competency.SortOrder))
            {
                errors.Add($"{path}.sortOrder {competency.SortOrder} is duplicated.");
            }

            if (slug is null)
            {
                continue;
            }

            if (!packSlugs.Add(slug))
            {
                errors.Add($"{path}.slug '{slug}' is duplicated in the pack.");
            }

            if (existingModules.TryGetValue(slug, out var existingModule)
                && !string.Equals(existingModule, moduleSlug, StringComparison.Ordinal))
            {
                errors.Add(
                    $"{path}.slug '{slug}' already belongs to module '{existingModule}' "
                    + "and cannot be moved by an import.");
            }
        }
    }

    private static void ValidateMaterials(
        LearningModuleImport module,
        string modulePath,
        IReadOnlyDictionary<string, string> existingModules,
        HashSet<string> packSlugs,
        List<string> errors)
    {
        var materials = module.Materials ?? [];
        var moduleSlug = module.Slug ?? string.Empty;

        if (materials.Count > MaxMaterials)
        {
            errors.Add(
                $"{modulePath}.materials must contain at most {MaxMaterials} entries.");
        }

        var sortOrders = new HashSet<int>();

        for (var index = 0; index < materials.Count; index++)
        {
            var material = materials[index];
            var path = $"{modulePath}.materials[{index}]";
            var slug = ValidateSlug(material.Slug, $"{path}.slug", 140, errors);

            ValidateRequiredText(material.Title, $"{path}.title", 200, errors);
            ValidateRequiredText(material.Kind, $"{path}.kind", 40, errors);
            ValidateEstimatedMinutes(
                material.EstimatedMinutes,
                $"{path}.estimatedMinutes",
                errors);
            ValidateSortOrder(material.SortOrder, $"{path}.sortOrder", errors);
            ValidateRequiredText(material.BodyMarkdown, $"{path}.bodyMarkdown", errors);
            ValidateMaxLength(
                material.BodyMarkdown,
                MaxMaterialBodyLength,
                $"{path}.bodyMarkdown",
                errors);

            if (!sortOrders.Add(material.SortOrder))
            {
                errors.Add($"{path}.sortOrder {material.SortOrder} is duplicated.");
            }

            if (slug is null)
            {
                continue;
            }

            if (!packSlugs.Add(slug))
            {
                errors.Add($"{path}.slug '{slug}' is duplicated in the pack.");
            }

            if (existingModules.TryGetValue(slug, out var existingModule)
                && !string.Equals(existingModule, moduleSlug, StringComparison.Ordinal))
            {
                errors.Add(
                    $"{path}.slug '{slug}' already belongs to module '{existingModule}' "
                    + "and cannot be moved by an import.");
            }
        }
    }

    private static void ValidatePrerequisites(
        LearningModuleImport module,
        string modulePath,
        string? moduleSlug,
        List<string> errors)
    {
        var prerequisites = module.Prerequisites ?? [];

        if (prerequisites.Count > MaxPrerequisites)
        {
            errors.Add(
                $"{modulePath}.prerequisites must contain at most {MaxPrerequisites} entries.");
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);

        for (var index = 0; index < prerequisites.Count; index++)
        {
            var path = $"{modulePath}.prerequisites[{index}]";
            var prerequisiteSlug = ValidateSlug(prerequisites[index], path, 120, errors);

            if (prerequisiteSlug is null)
            {
                continue;
            }

            if (!seen.Add(prerequisiteSlug))
            {
                errors.Add($"{path} '{prerequisiteSlug}' is duplicated.");
            }

            if (moduleSlug is not null
                && string.Equals(prerequisiteSlug, moduleSlug, StringComparison.Ordinal))
            {
                errors.Add($"{path} must not reference the module itself.");
            }
        }
    }

    private static IReadOnlyList<LearningDomain> BuildSeedDomains(
        IReadOnlyList<LearningDomainImport> domainImports)
    {
        var domains = new List<LearningDomain>();
        var modulesBySlug = new Dictionary<string, LearningModule>(StringComparer.Ordinal);

        foreach (var domainImport in domainImports)
        {
            var domain = new LearningDomain
            {
                Slug = domainImport.Slug!,
                Name = domainImport.Name!,
                Description = domainImport.Description!,
                SortOrder = domainImport.SortOrder
            };

            foreach (var moduleImport in domainImport.Modules ?? [])
            {
                var module = new LearningModule
                {
                    Slug = moduleImport.Slug!,
                    Title = moduleImport.Title!,
                    Summary = moduleImport.Summary!,
                    Level = moduleImport.Level!,
                    EstimatedMinutes = moduleImport.EstimatedMinutes,
                    SortOrder = moduleImport.SortOrder,
                    Domain = domain
                };

                foreach (var objectiveImport in moduleImport.Objectives ?? [])
                {
                    module.Objectives.Add(new LearningObjective
                    {
                        Module = module,
                        Text = objectiveImport.Text!,
                        SortOrder = objectiveImport.SortOrder
                    });
                }

                foreach (var competencyImport in moduleImport.Competencies ?? [])
                {
                    module.Competencies.Add(new Competency
                    {
                        Module = module,
                        Slug = competencyImport.Slug!,
                        Name = competencyImport.Name!,
                        Description = competencyImport.Description!,
                        SortOrder = competencyImport.SortOrder
                    });
                }

                foreach (var materialImport in moduleImport.Materials ?? [])
                {
                    module.Materials.Add(new LearningMaterial
                    {
                        Module = module,
                        Slug = materialImport.Slug!,
                        Title = materialImport.Title!,
                        Kind = materialImport.Kind!,
                        EstimatedMinutes = materialImport.EstimatedMinutes,
                        SortOrder = materialImport.SortOrder,
                        BodyMarkdown = materialImport.BodyMarkdown!
                    });
                }

                domain.Modules.Add(module);
                modulesBySlug[module.Slug] = module;
            }

            domains.Add(domain);
        }

        foreach (var domainImport in domainImports)
        {
            foreach (var moduleImport in domainImport.Modules ?? [])
            {
                var module = modulesBySlug[moduleImport.Slug!];

                foreach (var prerequisiteSlug in moduleImport.Prerequisites ?? [])
                {
                    var prerequisite = modulesBySlug.TryGetValue(prerequisiteSlug, out var match)
                        ? match
                        : new LearningModule { Slug = prerequisiteSlug };

                    module.Prerequisites.Add(new LearningModulePrerequisite
                    {
                        Module = module,
                        PrerequisiteModule = prerequisite
                    });
                }
            }
        }

        return domains;
    }

    private static string? ValidateSlug(
        string? value,
        string path,
        int maxLength,
        List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add($"{path} is required.");
            return null;
        }

        if (value.Length > maxLength)
        {
            errors.Add($"{path} must be at most {maxLength} characters.");
            return null;
        }

        if (!SlugPattern.IsMatch(value))
        {
            errors.Add(
                $"{path} must use lowercase letters, numbers, and single hyphens.");
            return null;
        }

        return value;
    }

    private static void ValidateRequiredText(
        string? value,
        string path,
        int maxLength,
        List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add($"{path} is required.");
            return;
        }

        ValidateMaxLength(value, maxLength, path, errors);
    }

    private static void ValidateRequiredText(string? value, string path, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add($"{path} is required.");
        }
    }

    private static void ValidateMaxLength(
        string? value,
        int maxLength,
        string path,
        List<string> errors)
    {
        if (value is not null && value.Length > maxLength)
        {
            errors.Add($"{path} must be at most {maxLength} characters.");
        }
    }

    private static void ValidateLevel(string? value, string path, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add($"{path} is required.");
            return;
        }

        if (!Levels.Contains(value))
        {
            errors.Add($"{path} must be Beginner, Intermediate, or Advanced.");
        }
    }

    private static void ValidateEstimatedMinutes(int value, string path, List<string> errors)
    {
        if (value is < 1 or > 600)
        {
            errors.Add($"{path} must be between 1 and 600.");
        }
    }

    private static void ValidateSortOrder(int value, string path, List<string> errors)
    {
        if (value < 1)
        {
            errors.Add($"{path} must be at least 1.");
        }
    }
}
