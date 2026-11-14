namespace Psel.Modules.Learning.Domain;

/// <summary>
/// A top-level learning area such as programming fundamentals or databases.
/// </summary>
public sealed class LearningDomain
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Slug { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public ICollection<LearningModule> Modules { get; set; } = [];
}
