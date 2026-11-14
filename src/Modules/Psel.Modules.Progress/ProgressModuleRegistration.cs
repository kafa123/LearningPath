using System.Reflection;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Psel.Modules.Progress.Contracts;
using Psel.Modules.Progress.Endpoints;

namespace Psel.Modules.Progress;

/// <summary>
/// Composition entry point for progress aggregation and examination sessions.
/// Progress reads append-only assessment evidence; examination sessions are the
/// only new mutable learner state in this module.
/// </summary>
public static class ProgressModuleRegistration
{
    public static Assembly ModelAssembly => typeof(ProgressModuleRegistration).Assembly;

    public static IServiceCollection AddProgressModule(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<ExaminationService>();
        services.AddScoped<IExaminationPolicy>(
            serviceProvider => serviceProvider.GetRequiredService<ExaminationService>());
        services.AddScoped<ProgressService>();

        return services;
    }

    public static IEndpointRouteBuilder MapProgressEndpoints(this IEndpointRouteBuilder endpoints)
    {
        Endpoints.ProgressEndpoints.MapProgressEndpoints(endpoints);
        return endpoints;
    }
}
