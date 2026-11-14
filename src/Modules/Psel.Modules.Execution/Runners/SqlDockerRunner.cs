using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Psel.Modules.Execution.Docker;

namespace Psel.Modules.Execution.Runners;

/// <summary>
/// Runs a submitted SQL query against a disposable SQLite database inside a
/// short-lived Docker container. The schema, seed data, expectations, and the
/// trusted harness are all supplied by the server through the challenge
/// execution source; the client only supplies one <c>.sql</c> file.
/// </summary>
internal sealed class SqlDockerRunner(
    IDockerEngine docker,
    IOptions<ExecutionOptions> options,
    ILogger<SqlDockerRunner> logger) : ILanguageRunner
{
    internal const string HarnessFileName = "PselHarness.py";

    private const string RunCommand =
        "python3 -B /workspace/PselHarness.py";

    public string LanguageId => "sql";

    public int MaxSubmissionFiles => 1;

    public bool AcceptsFile(string path)
    {
        return path.EndsWith(".sql", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsReservedPath(string path)
    {
        return string.Equals(path, HarnessFileName, StringComparison.OrdinalIgnoreCase)
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
                $"The SQL runner cannot execute language '{request.LanguageId}'.");
        }

        if (string.IsNullOrWhiteSpace(request.HarnessSource))
        {
            return Failure(
                ExecutionStatus.ExecutionUnavailable,
                "This challenge has no SQL execution harness.");
        }

        if (request.Files.Count != 1)
        {
            return Failure(
                ExecutionStatus.InternalError,
                "A SQL submission must contain exactly one query file.");
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
                    settings.SqlImage,
                    $"psel-exec-{request.SubmissionId:N}",
                    workspace,
                    RunCommand,
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
            logger.LogError(exception, "Could not prepare the SQL execution workspace.");

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
            ["PYTHONDONTWRITEBYTECODE"] = "1",
            ["PYTHONPYCACHEPREFIX"] = "/tmp/pycache",
            ["PYTHONUNBUFFERED"] = "1",
            ["PYTHONIOENCODING"] = "utf-8",
            ["HOME"] = "/tmp"
        };
    }

    private static void WriteWorkspace(string workspace, ExecutionRequest request)
    {
        ExecutionWorkspace.WriteFiles(workspace, request.Files);

        File.WriteAllText(
            Path.Combine(workspace, HarnessFileName),
            request.HarnessSource!);

        TestManifestWriter.Write(workspace, request.Tests!);
    }

    private static bool IsCompilationFailure(DockerRunResult result)
    {
        var text = string.Concat(result.StandardError, "\n", result.StandardOutput);

        return text.Contains("PSEL_SQL_COMPILE_ERROR", StringComparison.Ordinal);
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
