namespace Psel.Modules.Learning.Domain;

/// <summary>
/// A concrete learning objective for a module.
/// </summary>
public sealed class LearningObjective
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ModuleId { get; set; }

    public LearningModule? Module { get; set; }

    public string Text { get; set; } = string.Empty;

    public int SortOrder { get; set; }
}
