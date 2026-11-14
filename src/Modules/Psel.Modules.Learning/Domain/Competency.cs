namespace Psel.Modules.Learning.Domain;

/// <summary>
/// A measurable skill associated with a module.
/// </summary>
public sealed class Competency
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ModuleId { get; set; }

    public LearningModule? Module { get; set; }

    public string Slug { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public int SortOrder { get; set; }
}
