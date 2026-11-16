using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Psel.Infrastructure.Persistence;

namespace Psel.Modules.Learning.Seed;

/// <summary>
/// Applies the learning seed catalog as an upsert after database migration.
/// Restarting the API adds or refreshes content without touching learner
/// submissions, assessments, progress evidence, or rubric reviews.
/// </summary>
public sealed class LearningDataSeeder(
    IServiceScopeFactory scopeFactory,
    ILogger<LearningDataSeeder> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PselDbContext>();
        var domains = LearningSeedData.Create();

        var result = await LearningSeedSynchronizer.SynchronizeAsync(
            dbContext,
            domains,
            cancellationToken);

        if (result.Total > 0)
        {
            logger.LogInformation(
                "Synchronized {DomainCount} learning domains: {AddedCount} added, {UpdatedCount} updated.",
                domains.Count,
                result.Added,
                result.Updated);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
