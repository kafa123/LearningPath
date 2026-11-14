namespace Psel.Modules.Challenges.Packages;

/// <summary>
/// One validated test owned by a challenge package. The identifier is stable
/// within a package and is the only test value the execution harness returns.
/// </summary>
public sealed record ChallengeExecutionTest(
    string Id,
    string Visibility,
    string Name,
    string? Description,
    string DataJson);

/// <summary>
/// Server-only test material for a challenge. Public and hidden tests are kept
/// together here and never placed in a client-facing contract.
/// </summary>
public sealed record ChallengeTestPlan(
    IReadOnlyList<ChallengeExecutionTest> Tests);

/// <summary>
/// Trusted server-side view of challenge execution material. Harness and test
/// source are never part of <see cref="Contracts.ChallengeDetail"/> and must not
/// be sent to clients.
/// </summary>
public interface IChallengeExecutionSource
{
    bool CanExecute(string challengeSlug, string languageId);

    string? GetHarness(string challengeSlug, string languageId);

    ChallengeTestPlan? GetTestPlan(string challengeSlug);
}
