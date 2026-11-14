using Psel.Modules.Challenges.Contracts;

namespace Psel.Modules.Challenges.Packages;

internal sealed class ChallengeCatalog(ChallengePackageLoader loader)
    : IChallengeCatalog, IChallengeExecutionSource
{
    private readonly Lazy<ChallengePackageSet> packages = new(
        loader.LoadPackages,
        LazyThreadSafetyMode.ExecutionAndPublication);

    public int EnsureLoaded() => packages.Value.Details.Count;

    public IReadOnlyList<ChallengeSummary> GetSummaries()
    {
        return packages.Value.Details
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
                    .Select(language => new ChallengeLanguageSummary(language.Id, language.Name))
                    .ToList(),
                detail.PrerequisiteSlugs,
                detail.CompetencySlugs))
            .ToList();
    }

    public ChallengeDetail? FindBySlug(string slug)
    {
        return packages.Value.Details.FirstOrDefault(
            detail => string.Equals(detail.Slug, slug, StringComparison.OrdinalIgnoreCase));
    }

    public bool CanExecute(string challengeSlug, string languageId)
    {
        return GetHarness(challengeSlug, languageId) is not null;
    }

    public string? GetHarness(string challengeSlug, string languageId)
    {
        if (packages.Value.Harnesses.TryGetValue(challengeSlug, out var languages)
            && languages.TryGetValue(languageId, out var harness))
        {
            return harness;
        }

        return null;
    }

    public ChallengeTestPlan? GetTestPlan(string challengeSlug)
    {
        return packages.Value.TestPlans.TryGetValue(challengeSlug, out var plan)
            ? plan
            : null;
    }
}
