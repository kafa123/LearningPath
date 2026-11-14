using System.Reflection;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Psel.Modules.Assessment.Endpoints;

namespace Psel.Modules.Assessment;

/// <summary>
/// Composition entry point for the assessment module. It owns deterministic
/// scoring, assessment persistence, and the permitted result projection.
/// </summary>
public static class AssessmentModuleRegistration
{
    public static Assembly ModelAssembly => typeof(AssessmentModuleRegistration).Assembly;

    public static IServiceCollection AddAssessmentModule(this IServiceCollection services)
    {
        services.AddScoped<AssessmentService>();
        return services;
    }

    public static IEndpointRouteBuilder MapAssessmentEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        AssessmentEndpoints.MapAssessmentEndpoints(endpoints);
        return endpoints;
    }
}
