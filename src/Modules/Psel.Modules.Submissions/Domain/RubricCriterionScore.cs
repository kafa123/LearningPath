namespace Psel.Modules.Submissions.Domain;

/// <summary>
/// Snapshot of one criterion score at review time. Max points and weight are
/// copied from the trusted rubric so an older review stays explainable if the
/// package's rubric changes later.
/// </summary>
public sealed class RubricCriterionScore
{
    private RubricCriterionScore()
    {
    }

    public Guid Id { get; private set; } = Guid.NewGuid();

    public Guid RubricReviewId { get; private set; }

    public string CriterionId { get; private set; } = string.Empty;

    public string Title { get; private set; } = string.Empty;

    public int Points { get; private set; }

    public int MaxPoints { get; private set; }

    public int Weight { get; private set; }

    public int SortOrder { get; private set; }

    public static RubricCriterionScore Create(
        string criterionId,
        string title,
        int points,
        int maxPoints,
        int weight,
        int sortOrder)
    {
        return new RubricCriterionScore
        {
            CriterionId = criterionId,
            Title = title,
            Points = points,
            MaxPoints = maxPoints,
            Weight = weight,
            SortOrder = sortOrder
        };
    }

    public void Update(
        string title,
        int points,
        int maxPoints,
        int weight,
        int sortOrder)
    {
        Title = title;
        Points = points;
        MaxPoints = maxPoints;
        Weight = weight;
        SortOrder = sortOrder;
    }
}
