namespace Psel.Modules.Progress.Domain;

/// <summary>
/// Links one submission to an examination session. The submission module owns
/// the submission row, so this table stores the identifier rather than a
/// cross-module foreign key.
/// </summary>
public sealed class ExaminationAttempt
{
    private ExaminationAttempt()
    {
    }

    public Guid Id { get; private set; } = Guid.NewGuid();

    public Guid ExaminationId { get; private set; }

    public ExaminationSession? Examination { get; private set; }

    public Guid SubmissionId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static ExaminationAttempt Create(
        Guid examinationId,
        Guid submissionId,
        DateTimeOffset now)
    {
        return new ExaminationAttempt
        {
            ExaminationId = examinationId,
            SubmissionId = submissionId,
            CreatedAtUtc = now
        };
    }
}
