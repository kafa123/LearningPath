using System.Threading.Channels;
using Microsoft.Extensions.Options;
using Psel.Modules.Execution;

namespace Psel.Modules.Submissions.Services;

/// <summary>
/// In-memory bounded queue of submission identifiers. The queue is the hand-off
/// between the HTTP request path and the execution worker; it contains no user
/// content, only identifiers.
/// </summary>
internal sealed class SubmissionQueue
{
    private readonly Channel<Guid> channel;

    public SubmissionQueue(IOptions<ExecutionOptions> options)
    {
        var capacity = Math.Max(1, options.Value.MaxQueuedExecutions);

        channel = Channel.CreateBounded<Guid>(
            new BoundedChannelOptions(capacity)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = false,
                SingleWriter = false
            });
    }

    public bool TryEnqueue(Guid submissionId) => channel.Writer.TryWrite(submissionId);

    public IAsyncEnumerable<Guid> ReadAllAsync(CancellationToken cancellationToken)
    {
        return channel.Reader.ReadAllAsync(cancellationToken);
    }
}
