using System.Globalization;
using Psel.Modules.Execution.Docker;

namespace Psel.Modules.Execution.Runners;

/// <summary>
/// Maps a raw Docker result to an execution status. Language runners supply the
/// predicate that recognizes their compiler diagnostics; everything else is
/// shared so every language reports the same failure categories.
/// </summary>
internal static class RunnerOutcomeMapper
{
    public static ExecutionOutcome Map(
        DockerRunResult result,
        Func<DockerRunResult, bool> isCompilationFailure)
    {
        var status = ExecutionStatus.InternalError;
        string? failure = null;

        if (result.Cancelled)
        {
            status = ExecutionStatus.Cancelled;
            failure = "Execution was cancelled.";
        }
        else if (result.DockerUnavailable)
        {
            status = ExecutionStatus.ExecutionUnavailable;
            failure = result.StartError
                ?? "Docker is not available. Install Docker Desktop and ensure the engine is running.";
        }
        else if (result.TimedOut)
        {
            status = ExecutionStatus.TimeLimitExceeded;
            failure = "Execution exceeded the configured time limit.";
        }
        else if (result.OutputLimitExceeded)
        {
            status = ExecutionStatus.OutputLimitExceeded;
            failure = "Execution produced more output than the configured limit.";
        }
        else if (result.OutOfMemory)
        {
            status = ExecutionStatus.MemoryLimitExceeded;
            failure = "Execution exceeded the configured memory limit.";
        }
        else if (result.ExitCode == 0)
        {
            status = ExecutionStatus.Succeeded;
        }
        else if (result.ExitCode is null)
        {
            status = ExecutionStatus.InternalError;
            failure = result.StartError ?? "The execution engine did not return an exit code.";
        }
        else if (isCompilationFailure(result))
        {
            status = ExecutionStatus.CompilationError;
            failure = "Compilation failed.";
        }
        else
        {
            status = ExecutionStatus.RuntimeError;
            failure = $"The program exited with code {result.ExitCode.Value.ToString(CultureInfo.InvariantCulture)}.";
        }

        return new ExecutionOutcome(
            status,
            result.ExitCode,
            result.DurationMs,
            result.StandardOutput,
            result.StandardError,
            failure,
            result.OutputLimitExceeded);
    }
}
