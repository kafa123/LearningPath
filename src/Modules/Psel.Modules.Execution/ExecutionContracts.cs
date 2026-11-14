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

/// <summary>
/// Read-only runtime diagnostics for the local Settings page. Only values that
/// are already server-owned and safe to display are included; Docker
/// executable paths, workspace roots, and container identities are omitted.
/// </summary>
public sealed record RuntimeLanguage(string Id, string Name, string Image);

public sealed record RuntimeLimits(
    int TimeLimitSeconds,
    int MemoryLimitMb,
    double CpuLimit,
    int PidsLimit,
    int MaxOutputBytes,
    int MaxConcurrentExecutions,
    int MaxQueuedExecutions);

public sealed record ExecutionRuntimeInfo(
    bool ExecutionEnabled,
    IReadOnlyList<RuntimeLanguage> Languages,
    RuntimeLimits Limits);
