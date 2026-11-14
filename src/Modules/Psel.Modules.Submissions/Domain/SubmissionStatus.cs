namespace Psel.Modules.Submissions.Domain;

public enum SubmissionStatus
{
    Queued,
    Running,
    Succeeded,
    CompilationError,
    RuntimeError,
    TimeLimitExceeded,
    MemoryLimitExceeded,
    OutputLimitExceeded,
    ExecutionUnavailable,
    InternalError,
    Interrupted
}
