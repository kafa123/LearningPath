using System.Net;
using System.Net.Http.Json;

namespace Psel.Api.Tests;

public sealed class HealthEndpointTests
{
    [Fact]
    public async Task GetHealth_ReturnsHealthyStatusWithDatabaseCheck()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/health", cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<HealthResponse>(cancellationToken);

        Assert.NotNull(payload);
        Assert.Equal("Healthy", payload.Status);
        Assert.False(string.IsNullOrWhiteSpace(payload.Version));
        Assert.Contains(
            payload.Checks,
            check => check.Name == "database" && check.Status == "Healthy");
    }

    [Fact]
    public async Task GetHealth_DoesNotExposeInternalDetails()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/health", cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain(factory.DatabasePath, body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Data Source", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Exception", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("StackTrace", body, StringComparison.OrdinalIgnoreCase);
    }

    private sealed record HealthResponse(
        string Status,
        DateTime TimestampUtc,
        string Version,
        double TotalDurationMs,
        HealthCheck[] Checks);

    private sealed record HealthCheck(string Name, string Status, double DurationMs);
}
