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

/// <summary>
/// One source file restored into the workspace. It contains only learner
/// input; harnesses, hidden tests, and reference solutions are never included.
/// </summary>
public sealed record SubmissionSourceFile(string Path, string Content);

/// <summary>
/// The most recent attempt for one challenge language. The workspace uses it to
/// restore the learner's saved code and last recorded result.
/// </summary>
public sealed record LatestSubmission(
    SubmissionDetail Submission,
    IReadOnlyList<SubmissionSourceFile> Files);

public sealed record RubricCriterionInput(string? Id, int? Points);

/// <summary>
/// A learner-supplied self-review. Only per-criterion points and a note are
/// accepted; totals, bands, and criterion metadata are server-owned.
/// </summary>
public sealed record SaveRubricReviewRequest(
    IReadOnlyList<RubricCriterionInput>? Criteria,
    string? Note);

public sealed record RubricCriterionReview(
    string Id,
    string Title,
    string? Description,
    int MaxPoints,
    int Weight,
    int Points);

public sealed record RubricReviewDetail(
    Guid SubmissionId,
    string ChallengeSlug,
    int Score,
    string Band,
    string? Note,
    IReadOnlyList<RubricCriterionReview> Criteria,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
