using Psel.Modules.Challenges.Contracts;

namespace Psel.Modules.Challenges.Packages;

/// <summary>
/// Read-only catalog of validated challenge packages. The catalog is loaded
/// once at startup and never exposes hidden tests or reference solutions.
/// </summary>
public interface IChallengeCatalog
{
    IReadOnlyList<ChallengeSummary> GetSummaries();

    ChallengeDetail? FindBySlug(string slug);
}
