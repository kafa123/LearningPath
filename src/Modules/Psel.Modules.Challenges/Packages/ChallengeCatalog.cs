using Psel.Modules.Challenges.Contracts;

namespace Psel.Modules.Challenges.Packages;

internal sealed class ChallengeCatalog(ChallengePackageLoader loader)
    : IChallengeCatalog, IChallengeExecutionSource
{
    private readonly object sync = new();
    private ChallengePackageSet? packages;

    public int EnsureLoaded() => GetPackages().Details.Count;

    /// <summary>
    /// Validates the package directory again and swaps the in-memory catalog.
    /// Requests keep using the previous snapshot until the new one is ready.
    /// </summary>
    public void Reload()
    {
        var loaded = loader.LoadPackages();

        lock (sync)
        {
            packages = loaded;
        }
    }

    public IReadOnlyList<ChallengeSummary> GetSummaries()
    {
        return GetPackages().Details
            .Select(detail => new ChallengeSummary(
                detail.Slug,
                detail.Title,
                detail.Summary,
                detail.DomainSlug,
                detail.Topic,
                detail.Difficulty,
                detail.Type,
                detail.EstimatedMinutes,
                detail.Languages
                    .Select(language => new ChallengeLanguageSummary(
                        language.Id,
                        language.Name,
                        language.CanExecute))
                    .ToList(),
                detail.PrerequisiteSlugs,
                detail.CompetencySlugs))
            .ToList();
    }

    public ChallengeDetail? FindBySlug(string slug)
    {
        return GetPackages().Details.FirstOrDefault(
            detail => string.Equals(detail.Slug, slug, StringComparison.OrdinalIgnoreCase));
    }

    public bool CanExecute(string challengeSlug, string languageId)
    {
        return GetHarness(challengeSlug, languageId) is not null;
    }

    public string? GetHarness(string challengeSlug, string languageId)
    {
        if (GetPackages().Harnesses.TryGetValue(challengeSlug, out var languages)
            && languages.TryGetValue(languageId, out var harness))
        {
            return harness;
        }

        return null;
    }

    public ChallengeTestPlan? GetTestPlan(string challengeSlug)
    {
        return GetPackages().TestPlans.TryGetValue(challengeSlug, out var plan)
            ? plan
            : null;
    }

    private ChallengePackageSet GetPackages()
    {
        var current = Volatile.Read(ref packages);

        if (current is not null)
        {
            return current;
        }

        lock (sync)
        {
            current = packages;

            if (current is null)
            {
                current = loader.LoadPackages();
                packages = current;
            }

            return current;
        }
    }
}
