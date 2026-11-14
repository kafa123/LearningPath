using Psel.Modules.Execution;
using Psel.Modules.Execution.Docker;

namespace Psel.Api.Tests;

/// <summary>
/// Recording Docker boundary used by runner contract tests. It captures the
/// generated workspace so tests can verify the server-controlled content
/// without starting a real container.
/// </summary>
internal sealed class RecordingDockerEngine : IDockerEngine
{
    public bool Available { get; set; } = true;

    public DockerRunResult Result { get; set; } = new(
        0,
        string.Empty,
        string.Empty,
        1,
        false,
        false,
        false,
        false,
        false,
        null);

    public DockerRunRequest? Request { get; private set; }

    public bool WorkspaceExistedDuringRun { get; private set; }

    public Dictionary<string, string> Files { get; } = new(StringComparer.Ordinal);

    public Task<bool> IsAvailableAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(Available);
    }

    public Task<DockerRunResult> RunAsync(
        DockerRunRequest request,
        CancellationToken cancellationToken)
    {
        Request = request;
        WorkspaceExistedDuringRun = Directory.Exists(request.WorkspacePath);

        if (WorkspaceExistedDuringRun)
        {
            foreach (var fullPath in Directory.EnumerateFiles(
                request.WorkspacePath,
                "*",
                SearchOption.AllDirectories))
            {
                var relativePath = Path.GetRelativePath(
                        request.WorkspacePath,
                        fullPath)
                    .Replace(Path.DirectorySeparatorChar, '/');

                Files[relativePath] = File.ReadAllText(fullPath);
            }
        }

        return Task.FromResult(Result);
    }

    public Task RemoveContainerAsync(
        string containerName,
        CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task RemoveStaleContainersAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
