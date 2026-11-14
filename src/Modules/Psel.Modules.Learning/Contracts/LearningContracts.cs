namespace Psel.Modules.Learning.Contracts;

/// <summary>
/// Safe, client-facing projections of learning content. Internal identifiers and
/// persistence details are deliberately not part of the HTTP contract.
/// </summary>
public sealed record DomainSummary(
    string Slug,
    string Name,
    string Description,
    int ModuleCount);

public sealed record LearningPathSummary(
    string Slug,
    string Name,
    string Description,
    IReadOnlyList<ModuleSummary> Modules);

public sealed record ModuleSummary(
    string Slug,
    string Title,
    string Summary,
    string Level,
    int EstimatedMinutes,
    IReadOnlyList<string> PrerequisiteSlugs);

public sealed record DomainReference(string Slug, string Name);

public sealed record ModuleReference(string Slug, string Title, string DomainSlug);

public sealed record MaterialSummary(
    string Slug,
    string Title,
    string Kind,
    int EstimatedMinutes);

public sealed record CompetencySummary(
    string Slug,
    string Name,
    string Description);

public sealed record ModuleDetail(
    string Slug,
    string Title,
    string Summary,
    string Level,
    int EstimatedMinutes,
    DomainReference Domain,
    IReadOnlyList<string> Objectives,
    IReadOnlyList<MaterialSummary> Materials,
    IReadOnlyList<CompetencySummary> Competencies,
    IReadOnlyList<ModuleReference> Prerequisites);

public sealed record MaterialDetail(
    string Slug,
    string Title,
    string Kind,
    int EstimatedMinutes,
    ModuleReference Module,
    string BodyMarkdown);
