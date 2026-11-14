using System.Collections.Concurrent;
using Psel.Modules.Execution;

namespace Psel.Api.Tests;

/// <summary>
/// Deterministic in-process runner used by API tests. It records every request
/// so tests can verify that the server supplied the harness and source files.
/// </summary>
public sealed class FakeLanguageRunner(string languageId = "csharp") : ILanguageRunner
{
    private readonly ConcurrentQueue<ExecutionRequest> requests = new();

    public string LanguageId => languageId;

    public int MaxSubmissionFiles =>
        string.Equals(LanguageId, "sql", StringComparison.OrdinalIgnoreCase) ? 1 : 10;

    public IReadOnlyCollection<ExecutionRequest> Requests => requests.ToArray();

    public Func<ExecutionRequest, ExecutionOutcome> Handler { get; set; } =
        request => new ExecutionOutcome(
            ExecutionStatus.Succeeded,
            ExitCode: 0,
            DurationMs: 12,
            StandardOutput: $"fake output for {request.SubmissionId}",
            StandardError: string.Empty,
            FailureMessage: null,
            OutputTruncated: false);

    public bool AcceptsFile(string path)
    {
        var extension = LanguageId.ToLowerInvariant() switch
        {
            "python" => ".py",
            "sql" => ".sql",
            _ => ".cs"
        };

        return path.EndsWith(extension, StringComparison.OrdinalIgnoreCase);
    }

    public Task<ExecutionOutcome> ExecuteAsync(
        ExecutionRequest request,
        CancellationToken cancellationToken)
    {
        requests.Enqueue(request);

        return Task.FromResult(Handler(request));
    }
}
