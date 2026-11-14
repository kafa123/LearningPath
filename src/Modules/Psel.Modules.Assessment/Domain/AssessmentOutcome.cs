namespace Psel.Modules.Assessment.Domain;

/// <summary>
/// Deterministic classification of one assessment attempt. Values mirror the
/// outcomes required by the product specification.
/// </summary>
public enum AssessmentOutcome
{
    Accepted,
    WrongAnswer,
    CompilationError,
    RuntimeError,
    TimeLimitExceeded,
    MemoryLimitExceeded,
    OutputLimitExceeded,
    InternalRunnerError
}
