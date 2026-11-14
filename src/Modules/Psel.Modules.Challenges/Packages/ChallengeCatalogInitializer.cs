using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Psel.Modules.Challenges.Packages;

internal sealed class ChallengeCatalogInitializer(
    ChallengeCatalog catalog,
    ChallengeLocation location,
    ILogger<ChallengeCatalogInitializer> logger) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        var count = catalog.EnsureLoaded();

        logger.LogInformation(
            "Loaded {ChallengeCount} challenge packages from {ChallengePath}.",
            count,
            location.Path);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
