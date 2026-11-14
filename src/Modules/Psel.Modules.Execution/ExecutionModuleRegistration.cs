using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Psel.Modules.Execution.Docker;
using Psel.Modules.Execution.Runners;

namespace Psel.Modules.Execution;

/// <summary>
/// Composition entry point for the execution module. The module owns language
/// runner selection, Docker access, and execution limits. It never depends on
/// submissions or the API host.
/// </summary>
public static class ExecutionModuleRegistration
{
    public static IServiceCollection AddExecutionModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<ExecutionOptions>(
            configuration.GetSection(ExecutionOptions.SectionName));

        services.AddSingleton<IDockerEngine, ProcessDockerEngine>();
        services.AddSingleton<ILanguageRunner, CSharpDockerRunner>();
        services.AddSingleton<ILanguageRunner, PythonDockerRunner>();
        services.AddSingleton<ILanguageRunner, SqlDockerRunner>();
        services.AddSingleton<ILanguageCatalog, LanguageCatalog>();
        services.AddHostedService<StaleContainerCleanupService>();

        return services;
    }

    public static IEndpointRouteBuilder MapExecutionEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/languages").WithTags("Languages");

        group.MapGet("", GetLanguages)
            .WithName("ListLanguages")
            .WithSummary("List the languages the server is configured to execute.");

        var runtimeGroup = endpoints.MapGroup("/api/runtime").WithTags("Runtime");

        runtimeGroup.MapGet("", GetRuntime)
            .WithName("GetRuntimeInfo")
            .WithSummary("Describe the safe subset of server-owned execution configuration.");

        return endpoints;
    }

    private static Ok<IReadOnlyList<SupportedLanguage>> GetLanguages(
        ILanguageCatalog catalog)
    {
        return TypedResults.Ok(catalog.GetLanguages());
    }

    private static Ok<ExecutionRuntimeInfo> GetRuntime(
        ILanguageCatalog catalog,
        IOptions<ExecutionOptions> options)
    {
        var settings = options.Value;
        var images = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["csharp"] = settings.CSharpImage,
            ["python"] = settings.PythonImage,
            ["sql"] = settings.SqlImage
        };
        var languages = catalog.GetLanguages()
            .Select(language => new RuntimeLanguage(
                language.Id,
                language.Name,
                images.TryGetValue(language.Id, out var image) ? image : "unconfigured"))
            .ToList();
        var limits = new RuntimeLimits(
            settings.TimeLimitSeconds,
            settings.MemoryLimitMb,
            settings.CpuLimit,
            settings.PidsLimit,
            settings.MaxOutputBytes,
            settings.MaxConcurrentExecutions,
            settings.MaxQueuedExecutions);

        return TypedResults.Ok(new ExecutionRuntimeInfo(settings.Enabled, languages, limits));
    }
}

internal sealed class StaleContainerCleanupService(
    IDockerEngine docker,
    IOptions<ExecutionOptions> options,
    ILogger<StaleContainerCleanupService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled)
        {
            return;
        }

        try
        {
            await docker.RemoveStaleContainersAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // The application is shutting down.
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Could not check for stale execution containers.");
        }
    }
}
