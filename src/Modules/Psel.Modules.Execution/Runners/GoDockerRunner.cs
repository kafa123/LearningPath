using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Psel.Modules.Execution.Docker;

namespace Psel.Modules.Execution.Runners;

/// <summary>
/// Builds and runs a submitted Go package inside a short-lived Docker
/// container. The server writes the trusted harness, module file, and test
/// manifest; the client only supplies validated source files.
/// </summary>
internal sealed class GoDockerRunner(
    IDockerEngine docker,
    IOptions<ExecutionOptions> options,
    ILogger<GoDockerRunner> logger) : ILanguageRunner
{
    internal const string HarnessFileName = "PselHarness.go";
    internal const string ModuleFileName = "go.mod";

    private const string ModuleContent = """
        module psel/submission

        go 1.27
        """;

    private const string BuildAndRunCommand =
        "cd /workspace && go build -trimpath -o /tmp/psel-submission . "
        + "&& /tmp/psel-submission";

    public string LanguageId => "go";

    public bool AcceptsFile(string path)
    {
        return path.EndsWith(".go", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsReservedPath(string path)
    {
        return string.Equals(path, HarnessFileName, StringComparison.OrdinalIgnoreCase)
            || string.Equals(path, ModuleFileName, StringComparison.OrdinalIgnoreCase)
            || string.Equals(path, TestManifestWriter.FileName, StringComparison.OrdinalIgnoreCase);
    }

    public async Task<ExecutionOutcome> ExecuteAsync(
        ExecutionRequest request,
        CancellationToken cancellationToken)
    {
        var settings = options.Value;

        if (!settings.Enabled)
        {
            return Unavailable("Execution is disabled by server configuration.");
        }

        if (!string.Equals(request.LanguageId, LanguageId, StringComparison.OrdinalIgnoreCase))
        {
            return Failure(
                ExecutionStatus.InternalError,
                $"The Go runner cannot execute language '{request.LanguageId}'.");
        }

        if (string.IsNullOrWhiteSpace(request.HarnessSource))
        {
            return Failure(
                ExecutionStatus.ExecutionUnavailable,
                "This challenge has no execution harness.");
        }

        if (request.Files.Count == 0)
        {
            return Failure(ExecutionStatus.InternalError, "No source files were provided.");
        }

        if (request.Tests is null || request.Tests.Count == 0)
        {
            return Failure(
                ExecutionStatus.ExecutionUnavailable,
                "This challenge has no tests to run.");
        }

        foreach (var file in request.Files)
        {
            if (!ExecutionFileRules.IsSafeRelativePath(file.Path, out var path)
                || !AcceptsFile(path)
                || IsReservedPath(path))
            {
                return Failure(ExecutionStatus.InternalError, "A source file path was not valid.");
            }
        }

        if (!await docker.IsAvailableAsync(cancellationToken))
        {
            return Unavailable(
                "Docker is not available. Install Docker Desktop and ensure the engine is running.");
        }

        var workspace = ExecutionWorkspace.Create(settings);

        try
        {
            WriteWorkspace(workspace, request);

            var result = await docker.RunAsync(
                new DockerRunRequest(
                    settings.GoImage,
                    $"psel-exec-{request.SubmissionId:N}",
                    workspace,
                    BuildAndRunCommand,
                    TimeSpan.FromSeconds(Math.Clamp(settings.TimeLimitSeconds, 5, 600)),
                    Math.Max(1024, settings.MaxOutputBytes),
                    CreateEnvironment()),
                cancellationToken);

            return RunnerOutcomeMapper.Map(result, IsCompilationFailure);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return new ExecutionOutcome(
                ExecutionStatus.Cancelled,
                null,
                0,
                string.Empty,
                string.Empty,
                "Execution was cancelled.",
                false);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            logger.LogError(exception, "Could not prepare the execution workspace.");

            return Failure(
                ExecutionStatus.InternalError,
                "The execution workspace could not be prepared.");
        }
        finally
        {
            ExecutionWorkspace.TryDelete(workspace);
        }
    }

    private static Dictionary<string, string> CreateEnvironment()
    {
        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["HOME"] = "/tmp",
            ["GOCACHE"] = "/tmp/gocache",
            ["GOPATH"] = "/tmp/go",
            ["GO111MODULE"] = "on",
            ["GOTOOLCHAIN"] = "local",
            ["GOPROXY"] = "off",
            ["CGO_ENABLED"] = "0"
        };
    }

    private static void WriteWorkspace(string workspace, ExecutionRequest request)
    {
        File.WriteAllText(
            Path.Combine(workspace, ModuleFileName),
            ModuleContent);

        ExecutionWorkspace.WriteFiles(workspace, request.Files);

        File.WriteAllText(
            Path.Combine(workspace, HarnessFileName),
            request.HarnessSource!);

        TestManifestWriter.Write(workspace, request.Tests!);
    }

    private static bool IsCompilationFailure(DockerRunResult result)
    {
        var text = string.Concat(result.StandardError, "\n", result.StandardOutput);

        return text.Contains("# psel/submission", StringComparison.Ordinal)
            || text.Contains("syntax error", StringComparison.OrdinalIgnoreCase)
            || text.Contains("undefined:", StringComparison.Ordinal)
            || text.Contains("declared and not used", StringComparison.Ordinal)
            || text.Contains("imported and not used", StringComparison.Ordinal)
            || text.Contains("missing return", StringComparison.Ordinal)
            || text.Contains("cannot use", StringComparison.Ordinal);
    }

    private static ExecutionOutcome Unavailable(string message)
    {
        return Failure(ExecutionStatus.ExecutionUnavailable, message);
    }

    private static ExecutionOutcome Failure(ExecutionStatus status, string message)
    {
        return new ExecutionOutcome(
            status,
            null,
            0,
            string.Empty,
            string.Empty,
            message,
            false);
    }
}
