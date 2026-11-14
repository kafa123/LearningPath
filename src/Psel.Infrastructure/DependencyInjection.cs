using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Psel.Infrastructure.Persistence;

namespace Psel.Infrastructure;

/// <summary>
/// Registers persistence and database health services.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment,
        params Assembly[] moduleAssemblies)
    {
        var configuredPath = configuration[
            $"{DatabaseOptions.SectionName}:{nameof(DatabaseOptions.Path)}"];

        var databasePath = DatabasePathResolver.Resolve(
            configuredPath,
            environment.ContentRootPath);

        services.AddSingleton(new DatabaseLocation(databasePath));
        services.AddSingleton(new ModuleAssemblyRegistry(moduleAssemblies));

        services.AddDbContext<PselDbContext>(options =>
            options.UseSqlite($"Data Source={databasePath}"));

        services.AddHealthChecks()
            .AddDbContextCheck<PselDbContext>(name: "database");

        services.AddHostedService<DatabaseInitializer>();

        return services;
    }
}
