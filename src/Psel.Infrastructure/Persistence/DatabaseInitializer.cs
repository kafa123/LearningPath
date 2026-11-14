using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Psel.Infrastructure.Persistence;

/// <summary>
/// Applies pending migrations during startup for the local single-instance
/// application. Multi-instance deployments must move this to a controlled step.
/// </summary>
public sealed class DatabaseInitializer(
    IServiceScopeFactory scopeFactory,
    DatabaseLocation databaseLocation,
    ILogger<DatabaseInitializer> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(databaseLocation.Path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PselDbContext>();

        await dbContext.Database.MigrateAsync(cancellationToken);

        logger.LogInformation("Database initialized.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
