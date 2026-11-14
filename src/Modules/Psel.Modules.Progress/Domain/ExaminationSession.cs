namespace Psel.Modules.Progress.Domain;

/// <summary>
/// One server-created examination attempt container. The server chooses the
/// variant and time limit from challenge metadata; clients cannot supply either.
/// </summary>
public sealed class ExaminationSession
{
    private ExaminationSession()
    {
    }

    public Guid Id { get; private set; } = Guid.NewGuid();

    public string ChallengeSlug { get; private set; } = string.Empty;

    public string VariantId { get; private set; } = string.Empty;

    public string VariantLabel { get; private set; } = string.Empty;

    public int? TimeLimitSeconds { get; private set; }

    public ExaminationStatus Status { get; private set; } = ExaminationStatus.InProgress;

    public DateTimeOffset StartedAtUtc { get; private set; }

    public DateTimeOffset? ExpiresAtUtc { get; private set; }

    public DateTimeOffset? CompletedAtUtc { get; private set; }

    public ICollection<ExaminationAttempt> Attempts { get; private set; } = [];

    public static ExaminationSession Start(
        string challengeSlug,
        string variantId,
        string variantLabel,
        int? timeLimitSeconds,
        DateTimeOffset now)
    {
        return new ExaminationSession
        {
            ChallengeSlug = challengeSlug,
            VariantId = variantId,
            VariantLabel = variantLabel,
            TimeLimitSeconds = timeLimitSeconds,
            StartedAtUtc = now,
            ExpiresAtUtc = timeLimitSeconds is null ? null : now.AddSeconds(timeLimitSeconds.Value)
        };
    }

    public bool HasExpired(DateTimeOffset now)
    {
        return Status == ExaminationStatus.InProgress
            && ExpiresAtUtc is { } expires
            && expires <= now;
    }

    public void Expire()
    {
        if (Status == ExaminationStatus.InProgress)
        {
            Status = ExaminationStatus.Expired;
            CompletedAtUtc ??= ExpiresAtUtc;
        }
    }

    public void Complete(DateTimeOffset now)
    {
        if (Status == ExaminationStatus.InProgress)
        {
            Status = ExaminationStatus.Completed;
            CompletedAtUtc = now;
        }
    }
}
