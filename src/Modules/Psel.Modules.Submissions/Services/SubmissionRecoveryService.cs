using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Psel.Infrastructure.Persistence;
using Psel.Modules.Submissions.Domain;

namespace Psel.Modules.Submissions.Services;

/// <summary>
/// Marks submissions left queued or running by a previous process as
/// interrupted. Runs after migrations and before the execution worker starts.
/// </summary>
internal sealed class SubmissionRecoveryService(
    IServiceScopeFactory scopeFactory,
    ILogger<SubmissionRecoveryService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PselDbContext>();
        var stale = await dbContext
            .Set<Submission>()
            .Where(candidate =>
                candidate.Status == SubmissionStatus.Queued
                || candidate.Status == SubmissionStatus.Running)
            .ToListAsync(cancellationToken);

        if (stale.Count == 0)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;

        foreach (var submission in stale)
        {
            submission.MarkInterrupted(now);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogWarning(
            "Marked {Count} submission(s) as interrupted after restart.",
            stale.Count);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
