using System.Reflection;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Psel.Modules.Submissions.Endpoints;
using Psel.Modules.Submissions.Services;

namespace Psel.Modules.Submissions;

/// <summary>
/// Composition entry point for the submissions module. It owns submission
/// persistence, validation, the in-memory queue, and the execution worker.
/// </summary>
public static class SubmissionModuleRegistration
{
    public static Assembly ModelAssembly => typeof(SubmissionModuleRegistration).Assembly;

    public static IServiceCollection AddSubmissionsModule(this IServiceCollection services)
    {
        services.AddSingleton<SubmissionQueue>();
        services.AddScoped<SubmissionService>();
        services.AddHostedService<SubmissionRecoveryService>();
        services.AddHostedService<SubmissionWorker>();

        return services;
    }

    public static IEndpointRouteBuilder MapSubmissionEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        SubmissionEndpoints.MapSubmissionEndpoints(endpoints);
        return endpoints;
    }
}
