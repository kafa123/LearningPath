using System.Text.Json.Serialization;

namespace Psel.Modules.Learning.Contracts;

/// <summary>
/// Content pack accepted by the local authoring endpoint. The shape mirrors the
/// seed catalog, but slugs decide what is inserted or updated.
/// </summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class LearningContentPack
{
    public int SchemaVersion { get; init; }

    public List<LearningDomainImport>? Domains { get; init; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class LearningDomainImport
{
    public string? Slug { get; init; }

    public string? Name { get; init; }

    public string? Description { get; init; }

    public int SortOrder { get; init; }

    public List<LearningModuleImport>? Modules { get; init; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class LearningModuleImport
{
    public string? Slug { get; init; }

    public string? Title { get; init; }

    public string? Summary { get; init; }

    public string? Level { get; init; }

    public int EstimatedMinutes { get; init; }

    public int SortOrder { get; init; }

    public List<LearningObjectiveImport>? Objectives { get; init; }

    public List<LearningCompetencyImport>? Competencies { get; init; }

    public List<LearningMaterialImport>? Materials { get; init; }

    public List<string>? Prerequisites { get; init; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class LearningObjectiveImport
{
    public int SortOrder { get; init; }

    public string? Text { get; init; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class LearningCompetencyImport
{
    public string? Slug { get; init; }

    public string? Name { get; init; }

    public string? Description { get; init; }

    public int SortOrder { get; init; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class LearningMaterialImport
{
    public string? Slug { get; init; }

    public string? Title { get; init; }

    public string? Kind { get; init; }

    public int EstimatedMinutes { get; init; }

    public int SortOrder { get; init; }

    public string? BodyMarkdown { get; init; }
}

public sealed record LearningContentImportResult(
    int Added,
    int Updated,
    int DomainCount,
    int ModuleCount);
