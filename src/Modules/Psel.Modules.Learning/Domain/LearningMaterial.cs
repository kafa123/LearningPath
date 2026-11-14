namespace Psel.Modules.Learning.Domain;

/// <summary>
/// A readable learning resource belonging to a module.
/// </summary>
public sealed class LearningMaterial
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ModuleId { get; set; }

    public LearningModule? Module { get; set; }

    public string Slug { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Kind { get; set; } = string.Empty;

    public int EstimatedMinutes { get; set; }

    public string BodyMarkdown { get; set; } = string.Empty;

    public int SortOrder { get; set; }
}
