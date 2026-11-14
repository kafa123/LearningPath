using System.Reflection;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Psel.Modules.Learning.Endpoints;
using Psel.Modules.Learning.Seed;

namespace Psel.Modules.Learning;

/// <summary>
/// Composition entry point for the learning module. The API host calls these
/// methods; other modules must not depend on this module's internals.
/// </summary>
public static class LearningModuleRegistration
{
    public static Assembly ModelAssembly => typeof(LearningModuleRegistration).Assembly;

    public static IServiceCollection AddLearningModule(this IServiceCollection services)
    {
        services.AddHostedService<LearningDataSeeder>();
        return services;
    }

    public static IEndpointRouteBuilder MapLearningEndpoints(this IEndpointRouteBuilder endpoints)
    {
        LearningEndpoints.MapLearningEndpoints(endpoints);
        return endpoints;
    }
}
