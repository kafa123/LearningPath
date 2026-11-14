using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Psel.Api.Health;

namespace Psel.Api.Endpoints;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHealthChecks("/api/health", new HealthCheckOptions
        {
            ResponseWriter = HealthResponseWriter.WriteAsync
        });

        return endpoints;
    }
}
