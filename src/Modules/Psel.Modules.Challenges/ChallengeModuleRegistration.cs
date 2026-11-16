using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Psel.Modules.Challenges.Endpoints;
using Psel.Modules.Challenges.Packages;
namespace Psel.Modules.Challenges;

/// <summary>
/// Composition entry point for the challenge module. Challenge content is
/// filesystem-backed and validated once at startup; the API host only consumes
/// this module's public registration methods.
/// </summary>
public static class ChallengeModuleRegistration
{
    public static IServiceCollection AddChallengesModule(this IServiceCollection services)
    {
        services.AddSingleton(serviceProvider =>
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var environment = serviceProvider.GetRequiredService<IHostEnvironment>();
            var options = configuration
                .GetSection(ChallengeOptions.SectionName)
                .Get<ChallengeOptions>() ?? new ChallengeOptions();

            return new ChallengeLocation(
                ChallengePathResolver.Resolve(options.Path, environment.ContentRootPath));
        });

        services.AddSingleton<ChallengePackageLoader>();
        services.AddSingleton<ChallengeCatalog>();
        services.AddSingleton<IChallengeCatalog>(
            serviceProvider => serviceProvider.GetRequiredService<ChallengeCatalog>());
        services.AddSingleton<IChallengeExecutionSource>(
            serviceProvider => serviceProvider.GetRequiredService<ChallengeCatalog>());
        services.AddScoped<ChallengePackageImporter>();
        services.AddHostedService<ChallengeCatalogInitializer>();

        return services;
    }

    public static IEndpointRouteBuilder MapChallengeEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        ChallengeEndpoints.MapChallengeEndpoints(endpoints);
        return endpoints;
    }

    public static IEndpointRouteBuilder MapChallengeAuthoringEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        ChallengeAuthoringEndpoints.MapChallengeAuthoringEndpoints(endpoints);
        return endpoints;
    }
}
