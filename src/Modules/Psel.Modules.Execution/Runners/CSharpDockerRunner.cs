using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Psel.Modules.Execution.Docker;

namespace Psel.Modules.Execution.Runners;

/// <summary>
/// Builds and runs a submitted C# project inside a short-lived Docker
/// container. The harness and test manifest are server-side content from the
/// challenge package and are never accepted from the client.
/// </summary>
internal sealed class CSharpDockerRunner(
    IDockerEngine docker,
    IOptions<ExecutionOptions> options,
    ILogger<CSharpDockerRunner> logger) : ILanguageRunner
{
    internal const string ProjectFileName = "Submission.csproj";
    internal const string HarnessFileName = "PselHarness.cs";

    private const string ProjectContent = """
        <Project Sdk="Microsoft.NET.Sdk">
          <PropertyGroup>
            <OutputType>Exe</OutputType>
            <TargetFramework>net10.0</TargetFramework>
            <ImplicitUsings>enable</ImplicitUsings>
            <Nullable>enable</Nullable>
            <AssemblyName>Submission</AssemblyName>
            <TreatWarningsAsErrors>false</TreatWarningsAsErrors>
            <Deterministic>true</Deterministic>
          </PropertyGroup>
        </Project>
        """;

    private const string BuildAndRunCommand =
        "dotnet build Submission.csproj -c Release -o /tmp/out --nologo "
        + "-p:BaseIntermediateOutputPath=/tmp/obj/ "
        + "-p:BaseOutputPath=/tmp/bin/ "
        + "&& dotnet /tmp/out/Submission.dll";

    public string LanguageId => "csharp";

    public bool AcceptsFile(string path)
    {
        return path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsReservedPath(string path)
    {
        return string.Equals(path, HarnessFileName, StringComparison.OrdinalIgnoreCase)
            || string.Equals(path, TestManifestWriter.FileName, StringComparison.OrdinalIgnoreCase)
            || string.Equals(path, ProjectFileName, StringComparison.OrdinalIgnoreCase);
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
                $"The C# runner cannot execute language '{request.LanguageId}'.");
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
                    settings.CSharpImage,
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
            ["DOTNET_CLI_HOME"] = "/tmp",
            ["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1",
            ["DOTNET_NOLOGO"] = "1",
            ["DOTNET_SKIP_FIRST_TIME_EXPERIENCE"] = "1",
            ["DOTNET_CLI_UI_LANGUAGE"] = "en",
            ["NUGET_PACKAGES"] = "/tmp/nuget",
            ["HOME"] = "/tmp"
        };
    }

    private static void WriteWorkspace(string workspace, ExecutionRequest request)
    {
        File.WriteAllText(
            Path.Combine(workspace, ProjectFileName),
            ProjectContent);

        ExecutionWorkspace.WriteFiles(workspace, request.Files);

        File.WriteAllText(
            Path.Combine(workspace, HarnessFileName),
            request.HarnessSource!);

        TestManifestWriter.Write(workspace, request.Tests!);
    }

    private static bool IsCompilationFailure(DockerRunResult result)
    {
        var text = string.Concat(result.StandardError, "\n", result.StandardOutput);

        return text.Contains("error CS", StringComparison.Ordinal)
            || text.Contains("Build FAILED", StringComparison.Ordinal)
            || text.Contains("MSBUILD : error", StringComparison.OrdinalIgnoreCase);
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
