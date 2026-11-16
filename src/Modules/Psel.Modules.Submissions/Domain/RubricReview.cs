namespace Psel.Modules.Submissions.Domain;

/// <summary>
/// One advisory self-review of a completed submission against the challenge's
/// explicit rubric. The weighted score and band are always computed by the
/// server from server-owned criteria; they never replace deterministic
/// assessment results.
/// </summary>
public sealed class RubricReview
{
    private RubricReview()
    {
    }

    public Guid Id { get; private set; } = Guid.NewGuid();

    public Guid SubmissionId { get; private set; }

    public string ChallengeSlug { get; private set; } = string.Empty;

    public int Score { get; private set; }

    public string Band { get; private set; } = string.Empty;

    public string? Note { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public ICollection<RubricCriterionScore> CriterionScores { get; private set; } =
        new List<RubricCriterionScore>();

    public static RubricReview Create(
        Guid submissionId,
        string challengeSlug,
        int score,
        string band,
        string? note,
        DateTimeOffset now)
    {
        return new RubricReview
        {
            SubmissionId = submissionId,
            ChallengeSlug = challengeSlug,
            Score = score,
            Band = band,
            Note = note,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
    }

    public void Update(int score, string band, string? note, DateTimeOffset now)
    {
        Score = score;
        Band = band;
        Note = note;
        UpdatedAtUtc = now;
    }
}
