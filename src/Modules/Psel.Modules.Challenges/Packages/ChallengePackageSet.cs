namespace Psel.Modules.Challenges.Packages;

/// <summary>
/// The validated package content. Client-facing details are kept separate from
/// server-only execution material so a harness or hidden test can never be
/// serialized into an ordinary challenge response by accident.
/// </summary>
internal sealed record ChallengePackageSet(
    IReadOnlyList<Contracts.ChallengeDetail> Details,
    IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> Harnesses,
    IReadOnlyDictionary<string, ChallengeTestPlan> TestPlans);
