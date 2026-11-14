namespace Psel.Modules.Submissions.Contracts;

public sealed record SubmissionFileInput(string? Path, string? Content);

public sealed record CreateSubmissionRequest(
    string? ChallengeSlug,
    string? LanguageId,
    IReadOnlyList<SubmissionFileInput>? Files,
    string? Purpose = null,
    Guid? ExaminationId = null);

public sealed record SubmissionCreated(
    Guid Id,
    string Status,
    DateTimeOffset CreatedAtUtc);

public sealed record SubmissionDetail(
    Guid Id,
    string ChallengeSlug,
    string LanguageId,
    string Status,
    bool IsTerminal,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? StartedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    long? DurationMs,
    int? ExitCode,
    string StandardOutput,
    string StandardError,
    string? FailureMessage,
    bool OutputTruncated,
    string Purpose);
