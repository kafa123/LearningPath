namespace Psel.Modules.Learning.Domain;

/// <summary>
/// A group of related lessons, objectives, and competencies inside a domain.
/// </summary>
public sealed class LearningModule
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid DomainId { get; set; }

    public LearningDomain? Domain { get; set; }

    public string Slug { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;

    public string Level { get; set; } = string.Empty;

    public int EstimatedMinutes { get; set; }

    public int SortOrder { get; set; }

    public ICollection<LearningObjective> Objectives { get; set; } = [];

    public ICollection<LearningMaterial> Materials { get; set; } = [];

    public ICollection<Competency> Competencies { get; set; } = [];

    public ICollection<LearningModulePrerequisite> Prerequisites { get; set; } = [];
}
