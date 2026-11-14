namespace Psel.Modules.Execution;

public enum ExecutionStatus
{
    Succeeded,
    CompilationError,
    RuntimeError,
    TimeLimitExceeded,
    MemoryLimitExceeded,
    OutputLimitExceeded,
    ExecutionUnavailable,
    InternalError,
    Cancelled
}

public sealed record ExecutionFile(string Path, string Content);

/// <summary>
/// One server-selected test from a challenge package. The opaque identifier is
/// the only value carried back from the harness; names, descriptions, and test
/// content stay in server memory.
/// </summary>
public sealed record ExecutionTest(
    string Id,
    string Visibility,
    string DataJson);

public sealed record ExecutionRequest(
    Guid SubmissionId,
    string ChallengeSlug,
    string LanguageId,
    IReadOnlyList<ExecutionFile> Files,
    string? HarnessSource,
    IReadOnlyList<ExecutionTest>? Tests = null);

public sealed record ExecutionOutcome(
    ExecutionStatus Status,
    int? ExitCode,
    long DurationMs,
    string StandardOutput,
    string StandardError,
    string? FailureMessage,
    bool OutputTruncated);

public sealed record SupportedLanguage(string Id, string Name);
