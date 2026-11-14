using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Psel.Infrastructure.Persistence;
using Psel.Modules.Learning.Domain;

namespace Psel.Modules.Learning.Seed;

/// <summary>
/// Seeds the initial learning catalog once. The seeder runs after database
/// migration and is idempotent for the local single-user application.
/// </summary>
public sealed class LearningDataSeeder(
    IServiceScopeFactory scopeFactory,
    ILogger<LearningDataSeeder> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PselDbContext>();

        if (await dbContext.Set<LearningDomain>().AnyAsync(cancellationToken))
        {
            return;
        }

        var domains = LearningSeedData.Create();
        dbContext.AddRange(domains);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Seeded {DomainCount} learning domains with modules and materials.",
            domains.Count);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
