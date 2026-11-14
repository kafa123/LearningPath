namespace Psel.Modules.Assessment.Domain;

/// <summary>
/// Evidence for one test selected for the attempt. Hidden test metadata is
/// persisted for internal diagnosis but is filtered out of client projections.
/// </summary>
public sealed class AssessmentTestResult
{
    private AssessmentTestResult()
    {
    }

    public Guid Id { get; private set; } = Guid.NewGuid();

    public Guid AssessmentId { get; private set; }

    public int SortOrder { get; private set; }

    public string TestId { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public AssessmentVisibility Visibility { get; private set; }

    public AssessmentTestStatus Status { get; private set; }

    public static AssessmentTestResult Create(
        int sortOrder,
        string testId,
        string name,
        string? description,
        AssessmentVisibility visibility,
        AssessmentTestStatus status)
    {
        return new AssessmentTestResult
        {
            SortOrder = sortOrder,
            TestId = testId,
            Name = name,
            Description = description,
            Visibility = visibility,
            Status = status
        };
    }
}
