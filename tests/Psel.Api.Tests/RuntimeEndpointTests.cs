using System.Net;
using System.Net.Http.Json;

namespace Psel.Api.Tests;

public sealed class RuntimeEndpointTests
{
    [Fact]
    public async Task GetRuntime_ReturnsSafeExecutionDiagnostics()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/runtime", cancellationToken);
        var runtime = await response.Content.ReadFromJsonAsync<RuntimeResponse>(cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(runtime);
        Assert.False(runtime.ExecutionEnabled);
        Assert.Equal(["csharp", "python", "sql"], runtime.Languages.Select(language => language.Id));
        Assert.All(runtime.Languages, language => Assert.False(string.IsNullOrWhiteSpace(language.Image)));
        Assert.Equal(60, runtime.Limits.TimeLimitSeconds);
        Assert.Equal(512, runtime.Limits.MemoryLimitMb);
        Assert.Equal(1.0, runtime.Limits.CpuLimit);
        Assert.Equal(128, runtime.Limits.PidsLimit);
        Assert.Equal(64 * 1024, runtime.Limits.MaxOutputBytes);
        Assert.Equal(2, runtime.Limits.MaxConcurrentExecutions);
        Assert.Equal(64, runtime.Limits.MaxQueuedExecutions);
    }

    [Fact]
    public async Task GetRuntime_DoesNotExposeServerInternals()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/runtime", cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain("dockerExecutable", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("workspaceRoot", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("containerUser", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(factory.DatabasePath, body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Data Source", body, StringComparison.OrdinalIgnoreCase);
    }

    private sealed record RuntimeResponse(
        bool ExecutionEnabled,
        RuntimeLanguage[] Languages,
        RuntimeLimits Limits);

    private sealed record RuntimeLanguage(string Id, string Name, string Image);

    private sealed record RuntimeLimits(
        int TimeLimitSeconds,
        int MemoryLimitMb,
        double CpuLimit,
        int PidsLimit,
        int MaxOutputBytes,
        int MaxConcurrentExecutions,
        int MaxQueuedExecutions);
}
