using Psel.Modules.Execution;

namespace Psel.Modules.Submissions.Domain;

/// <summary>
/// An append-only record of one execution attempt. A new attempt creates a new
/// row; previous attempts are never overwritten. Status and output fields are
/// updated only for the lifecycle of this row.
/// </summary>
public sealed class Submission
{
    private Submission()
    {
    }

    public Guid Id { get; private set; } = Guid.NewGuid();

    public string ChallengeSlug { get; private set; } = string.Empty;

    public string LanguageId { get; private set; } = string.Empty;

    public SubmissionPurpose Purpose { get; private set; } = SubmissionPurpose.Run;

    public SubmissionStatus Status { get; private set; } = SubmissionStatus.Queued;

    public string SourceFilesJson { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset? StartedAtUtc { get; private set; }

    public DateTimeOffset? CompletedAtUtc { get; private set; }

    public long? DurationMs { get; private set; }

    public int? ExitCode { get; private set; }

    public string StandardOutput { get; private set; } = string.Empty;

    public string StandardError { get; private set; } = string.Empty;

    public string? FailureMessage { get; private set; }

    public bool OutputTruncated { get; private set; }

    public bool IsTerminal => Status is not (SubmissionStatus.Queued or SubmissionStatus.Running);

    public static Submission Create(
        string challengeSlug,
        string languageId,
        SubmissionPurpose purpose,
        string sourceFilesJson,
        DateTimeOffset now)
    {
        return new Submission
        {
            ChallengeSlug = challengeSlug,
            LanguageId = languageId,
            Purpose = purpose,
            SourceFilesJson = sourceFilesJson,
            Status = SubmissionStatus.Queued,
            CreatedAtUtc = now
        };
    }

    public void MarkRunning(DateTimeOffset now)
    {
        if (Status != SubmissionStatus.Queued)
        {
            throw new InvalidOperationException(
                $"Submission {Id} cannot start from status {Status}.");
        }

        Status = SubmissionStatus.Running;
        StartedAtUtc = now;
    }

    public void Complete(ExecutionOutcome outcome, DateTimeOffset now)
    {
        if (IsTerminal)
        {
            return;
        }

        Status = outcome.Status switch
        {
            ExecutionStatus.Succeeded => SubmissionStatus.Succeeded,
            ExecutionStatus.CompilationError => SubmissionStatus.CompilationError,
            ExecutionStatus.RuntimeError => SubmissionStatus.RuntimeError,
            ExecutionStatus.TimeLimitExceeded => SubmissionStatus.TimeLimitExceeded,
            ExecutionStatus.MemoryLimitExceeded => SubmissionStatus.MemoryLimitExceeded,
            ExecutionStatus.OutputLimitExceeded => SubmissionStatus.OutputLimitExceeded,
            ExecutionStatus.ExecutionUnavailable => SubmissionStatus.ExecutionUnavailable,
            ExecutionStatus.Cancelled => SubmissionStatus.Interrupted,
            _ => SubmissionStatus.InternalError
        };

        StartedAtUtc ??= now;
        CompletedAtUtc = now;
        DurationMs = outcome.DurationMs;
        ExitCode = outcome.ExitCode;
        StandardOutput = outcome.StandardOutput;
        StandardError = outcome.StandardError;
        FailureMessage = outcome.FailureMessage;
        OutputTruncated = outcome.OutputTruncated;
    }

    public void MarkInterrupted(DateTimeOffset now)
    {
        if (IsTerminal)
        {
            return;
        }

        Status = SubmissionStatus.Interrupted;
        StartedAtUtc ??= now;
        CompletedAtUtc = now;
        FailureMessage = "Execution was interrupted before it completed.";
    }

    public void MarkUnavailable(string message, DateTimeOffset now)
    {
        if (IsTerminal)
        {
            return;
        }

        Status = SubmissionStatus.ExecutionUnavailable;
        CompletedAtUtc = now;
        FailureMessage = message;
    }
}
