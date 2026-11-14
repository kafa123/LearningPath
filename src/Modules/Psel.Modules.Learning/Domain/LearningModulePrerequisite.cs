namespace Psel.Modules.Learning.Domain;

/// <summary>
/// A prerequisite relationship between two learning modules.
/// </summary>
public sealed class LearningModulePrerequisite
{
    public Guid ModuleId { get; set; }

    public LearningModule? Module { get; set; }

    public Guid PrerequisiteModuleId { get; set; }

    public LearningModule? PrerequisiteModule { get; set; }
}
