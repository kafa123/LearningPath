using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Psel.Infrastructure.Persistence;

namespace Psel.Api.Tests;

public sealed class DatabaseInitializationTests
{
    [Fact]
    public async Task Startup_AppliesMigrationsToCleanDatabase()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/health", cancellationToken);
        response.EnsureSuccessStatusCode();

        Assert.True(File.Exists(factory.DatabasePath));

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PselDbContext>();

        Assert.True(await dbContext.Database.CanConnectAsync(cancellationToken));

        var appliedMigrations = await dbContext.Database.GetAppliedMigrationsAsync(cancellationToken);
        Assert.Contains(
            appliedMigrations,
            migration => migration.EndsWith("_InitialCreate", StringComparison.Ordinal));
    }

    [Fact]
    public async Task HealthEndpoint_CanBeCalledRepeatedlyWithoutReapplyingMigrations()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        using var first = await client.GetAsync("/api/health", cancellationToken);
        using var second = await client.GetAsync("/api/health", cancellationToken);

        first.EnsureSuccessStatusCode();
        second.EnsureSuccessStatusCode();

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PselDbContext>();
        var appliedMigrations = (await dbContext.Database.GetAppliedMigrationsAsync(cancellationToken))
            .ToList();

        Assert.Equal(appliedMigrations.Count, appliedMigrations.Distinct().Count());
        Assert.Contains(
            appliedMigrations,
            migration => migration.EndsWith("_InitialCreate", StringComparison.Ordinal));
        Assert.Contains(
            appliedMigrations,
            migration => migration.EndsWith("_LearningModel", StringComparison.Ordinal));
    }
}
