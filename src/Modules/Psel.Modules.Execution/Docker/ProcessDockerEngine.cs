using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Psel.Modules.Execution.Docker;

public sealed record DockerRunRequest(
    string Image,
    string ContainerName,
    string WorkspacePath,
    string Command,
    TimeSpan Timeout,
    int MaxOutputBytes,
    IReadOnlyDictionary<string, string> EnvironmentVariables);

public sealed record DockerRunResult(
    int? ExitCode,
    string StandardOutput,
    string StandardError,
    long DurationMs,
    bool TimedOut,
    bool OutputLimitExceeded,
    bool DockerUnavailable,
    bool OutOfMemory,
    bool Cancelled,
    string? StartError);

/// <summary>
/// Narrow Docker boundary. The runner supplies only the image, workspace, and
/// server-controlled command; isolation flags live in this implementation.
/// </summary>
public interface IDockerEngine
{
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken);

    Task<DockerRunResult> RunAsync(
        DockerRunRequest request,
        CancellationToken cancellationToken);

    Task RemoveContainerAsync(string containerName, CancellationToken cancellationToken);

    Task RemoveStaleContainersAsync(CancellationToken cancellationToken);
}

internal sealed class ProcessDockerEngine(
    IOptions<ExecutionOptions> options,
    ILogger<ProcessDockerEngine> logger) : IDockerEngine
{
    private const string ExecutionLabel = "psel.execution=true";

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken)
    {
        if (!options.Value.Enabled)
        {
            return false;
        }

        try
        {
            var result = await RunProcessAsync(
                ["version", "--format", "{{.Server.Version}}"],
                TimeSpan.FromSeconds(10),
                4096,
                cancellationToken);

            return result.ExitCode == 0
                && !result.TimedOut
                && !result.Cancelled
                && !result.DockerUnavailable;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return false;
        }
    }

    public async Task<DockerRunResult> RunAsync(
        DockerRunRequest request,
        CancellationToken cancellationToken)
    {
        if (!options.Value.Enabled)
        {
            return Unavailable("Execution is disabled by server configuration.");
        }

        var arguments = BuildRunArguments(request, options.Value);
        ProcessOutcome outcome;

        try
        {
            outcome = await RunProcessAsync(
                arguments,
                request.Timeout,
                request.MaxOutputBytes,
                cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Docker execution failed to start for {ContainerName}.", request.ContainerName);

            await RemoveContainerAsync(request.ContainerName, CancellationToken.None);

            return new DockerRunResult(
                null,
                string.Empty,
                string.Empty,
                0,
                false,
                false,
                false,
                false,
                false,
                "The execution engine could not be started.");
        }

        var outOfMemory = false;

        if (!outcome.TimedOut
            && !outcome.Cancelled
            && !outcome.DockerUnavailable
            && outcome.ExitCode is not null)
        {
            outOfMemory = await InspectOutOfMemoryAsync(
                request.ContainerName,
                cancellationToken);
        }

        await RemoveContainerAsync(request.ContainerName, CancellationToken.None);

        return new DockerRunResult(
            outcome.ExitCode,
            outcome.StandardOutput,
            outcome.StandardError,
            outcome.DurationMs,
            outcome.TimedOut,
            outcome.OutputLimitExceeded,
            outcome.DockerUnavailable,
            outOfMemory,
            outcome.Cancelled,
            outcome.StartError);
    }

    public async Task RemoveContainerAsync(
        string containerName,
        CancellationToken cancellationToken)
    {
        try
        {
            await RunProcessAsync(
                ["rm", "-f", containerName],
                TimeSpan.FromSeconds(10),
                4096,
                cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(
                exception,
                "Could not remove execution container {ContainerName}.",
                containerName);
        }
    }

    public async Task RemoveStaleContainersAsync(CancellationToken cancellationToken)
    {
        if (!options.Value.Enabled)
        {
            return;
        }

        var listed = await RunProcessAsync(
            ["ps", "-aq", "--filter", $"label={ExecutionLabel}"],
            TimeSpan.FromSeconds(10),
            64 * 1024,
            cancellationToken);

        if (listed.DockerUnavailable || listed.ExitCode != 0)
        {
            return;
        }

        var containers = listed.StandardOutput
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (containers.Count == 0)
        {
            return;
        }

        logger.LogWarning(
            "Removing {Count} execution container(s) left behind by a previous run.",
            containers.Count);

        foreach (var container in containers)
        {
            await RemoveContainerAsync(container, cancellationToken);
        }
    }

    internal static IReadOnlyList<string> BuildRunArguments(
        DockerRunRequest request,
        ExecutionOptions settings)
    {
        var arguments = new List<string>
        {
            "run",
            "--name",
            request.ContainerName,
            "--label",
            ExecutionLabel,
            "--network",
            "none",
            "--memory",
            $"{settings.MemoryLimitMb}m",
            "--memory-swap",
            $"{settings.MemoryLimitMb}m",
            "--cpus",
            settings.CpuLimit.ToString(CultureInfo.InvariantCulture),
            "--pids-limit",
            settings.PidsLimit.ToString(CultureInfo.InvariantCulture),
            "--read-only",
            "--tmpfs",
            "/tmp:rw,nosuid,nodev,size=256m",
            "--cap-drop",
            "ALL",
            "--security-opt",
            "no-new-privileges",
            "--user",
            settings.ContainerUser,
            "--volume",
            $"{request.WorkspacePath}:/workspace:ro",
            "--workdir",
            "/workspace"
        };

        foreach (var variable in request.EnvironmentVariables)
        {
            arguments.Add("--env");
            arguments.Add($"{variable.Key}={variable.Value}");
        }

        arguments.Add(request.Image);
        arguments.Add("/bin/sh");
        arguments.Add("-c");
        arguments.Add(request.Command);

        return arguments;
    }

    private async Task<bool> InspectOutOfMemoryAsync(
        string containerName,
        CancellationToken cancellationToken)
    {
        try
        {
            var inspected = await RunProcessAsync(
                ["inspect", "--format", "{{.State.OOMKilled}}", containerName],
                TimeSpan.FromSeconds(5),
                1024,
                cancellationToken);

            return inspected.ExitCode == 0
                && inspected.StandardOutput.Contains("true", StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(
                exception,
                "Could not inspect execution container {ContainerName}.",
                containerName);

            return false;
        }
    }

    private async Task<ProcessOutcome> RunProcessAsync(
        IReadOnlyList<string> arguments,
        TimeSpan timeout,
        int maxOutputBytes,
        CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = options.Value.DockerExecutable,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = new Process { StartInfo = startInfo };

        try
        {
            process.Start();
        }
        catch (Win32Exception)
        {
            return new ProcessOutcome(
                null,
                string.Empty,
                string.Empty,
                0,
                false,
                false,
                true,
                false,
                "Docker is not available. Install Docker Desktop and ensure the engine is running.");
        }

        var stopwatch = Stopwatch.StartNew();
        var budget = new OutputBudget(Math.Max(1024, maxOutputBytes));
        var output = new OutputCollector(budget);
        var error = new OutputCollector(budget);
        budget.LimitReached = () => Kill(process);

        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(timeout);

        var readOutput = output.ReadAsync(process.StandardOutput);
        var readError = error.ReadAsync(process.StandardError);
        var timedOut = false;
        var cancelled = false;

        try
        {
            await process.WaitForExitAsync(timeoutSource.Token);
        }
        catch (OperationCanceledException)
        {
            cancelled = cancellationToken.IsCancellationRequested;
            timedOut = !cancelled;
            Kill(process);
        }

        await Task.WhenAll(readOutput, readError);

        stopwatch.Stop();

        int? exitCode = null;

        try
        {
            if (process.HasExited)
            {
                exitCode = process.ExitCode;
            }
        }
        catch (InvalidOperationException)
        {
            // The process never started or already exited.
        }

        var standardError = error.Text;
        var dockerUnavailable = LooksLikeDockerUnavailable(standardError);

        return new ProcessOutcome(
            exitCode,
            output.Text,
            standardError,
            stopwatch.ElapsedMilliseconds,
            timedOut,
            budget.LimitHit,
            dockerUnavailable,
            cancelled,
            null);
    }

    private static void Kill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (InvalidOperationException)
        {
            // The process already exited.
        }
        catch (NotSupportedException)
        {
            // The platform does not support process-tree termination.
        }
    }

    private static bool LooksLikeDockerUnavailable(string text)
    {
        return text.Contains("Cannot connect to the Docker daemon", StringComparison.OrdinalIgnoreCase)
            || text.Contains("Is the docker daemon running", StringComparison.OrdinalIgnoreCase)
            || text.Contains("error during connect", StringComparison.OrdinalIgnoreCase)
            || text.Contains("docker: command not found", StringComparison.OrdinalIgnoreCase);
    }

    private static DockerRunResult Unavailable(string message)
    {
        return new DockerRunResult(
            null,
            string.Empty,
            string.Empty,
            0,
            false,
            false,
            true,
            false,
            false,
            message);
    }

    private sealed record ProcessOutcome(
        int? ExitCode,
        string StandardOutput,
        string StandardError,
        long DurationMs,
        bool TimedOut,
        bool OutputLimitExceeded,
        bool DockerUnavailable,
        bool Cancelled,
        string? StartError);

    private sealed class OutputCollector(OutputBudget budget)
    {
        private readonly StringBuilder builder = new();
        private readonly Lock sync = new();

        public string Text
        {
            get
            {
                lock (sync)
                {
                    return builder.ToString();
                }
            }
        }

        public async Task ReadAsync(StreamReader reader)
        {
            var buffer = new char[4096];

            while (true)
            {
                var read = await reader.ReadAsync(buffer.AsMemory(), CancellationToken.None);

                if (read == 0)
                {
                    break;
                }

                var allowed = budget.Reserve(read);

                if (allowed == 0)
                {
                    continue;
                }

                lock (sync)
                {
                    builder.Append(buffer, 0, allowed);
                }
            }
        }
    }

    private sealed class OutputBudget(int maximum)
    {
        private int total;
        private int limitHit;

        public Action? LimitReached { get; set; }

        public bool LimitHit => Volatile.Read(ref limitHit) == 1;

        public int Reserve(int requested)
        {
            while (true)
            {
                var used = Volatile.Read(ref total);

                if (used >= maximum)
                {
                    MarkLimit();
                    return 0;
                }

                var take = Math.Min(requested, maximum - used);

                if (Interlocked.CompareExchange(ref total, used + take, used) == used)
                {
                    if (take < requested)
                    {
                        MarkLimit();
                    }

                    return take;
                }
            }
        }

        private void MarkLimit()
        {
            if (Interlocked.Exchange(ref limitHit, 1) == 0)
            {
                LimitReached?.Invoke();
            }
        }
    }
}
