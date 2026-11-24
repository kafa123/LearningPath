namespace Psel.Modules.Execution;

/// <summary>
/// Server-controlled execution limits and Docker configuration. Clients never
/// supply any of these values.
/// </summary>
public sealed class ExecutionOptions
{
    public const string SectionName = "Execution";

    public bool Enabled { get; set; } = true;

    public string DockerExecutable { get; set; } = "docker";

    public string CSharpImage { get; set; } = "mcr.microsoft.com/dotnet/sdk:10.0";

    public string PythonImage { get; set; } = "python:3.13-slim";

    /// <summary>
    /// Image used by the Go runner. The SDK image is required because the
    /// submission is compiled inside the container before it runs.
    /// </summary>
    public string GoImage { get; set; } = "golang:1.27-alpine";

    /// <summary>
    /// Image used by the SQL runner. SQLite ships with the Python standard
    /// library, so the trusted SQL harness runs in the same minimal image as
    /// Python submissions.
    /// </summary>
    public string SqlImage { get; set; } = "python:3.13-slim";

    /// <summary>
    /// Total wall-clock budget for one submission, including compilation,
    /// restore, or interpreter startup for the selected language.
    /// </summary>
    public int TimeLimitSeconds { get; set; } = 60;

    public int MemoryLimitMb { get; set; } = 512;

    public double CpuLimit { get; set; } = 1.0;

    public int PidsLimit { get; set; } = 128;

    public int MaxOutputBytes { get; set; } = 64 * 1024;

    public int MaxConcurrentExecutions { get; set; } = 2;

    public int MaxQueuedExecutions { get; set; } = 64;

    public string ContainerUser { get; set; } = "1000:1000";

    /// <summary>
    /// Optional directory for per-submission workspaces. Defaults to the OS
    /// temporary directory. The directory is created when needed.
    /// </summary>
    public string? WorkspaceRoot { get; set; }
}
