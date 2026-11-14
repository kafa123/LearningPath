using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Psel.Api.Health;

/// <summary>
/// Writes a JSON health response without exposing internal exception details,
/// paths, or connection information.
/// </summary>
public static class HealthResponseWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static Task WriteAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json; charset=utf-8";

        var payload = new
        {
            status = report.Status.ToString(),
            timestampUtc = DateTime.UtcNow,
            version = typeof(HealthResponseWriter).Assembly.GetName().Version?.ToString() ?? "unknown",
            totalDurationMs = Math.Round(report.TotalDuration.TotalMilliseconds, 2),
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                durationMs = Math.Round(entry.Value.Duration.TotalMilliseconds, 2)
            }).ToArray()
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(payload, JsonOptions));
    }
}
