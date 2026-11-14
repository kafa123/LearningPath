using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Psel.Modules.Challenges.Contracts;
using Psel.Modules.Challenges.Packages;

namespace Psel.Modules.Challenges.Endpoints;

internal static class ChallengeEndpoints
{
    public static IEndpointRouteBuilder MapChallengeEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/challenges").WithTags("Challenges");

        group.MapGet("", GetChallenges)
            .WithName("ListChallenges")
            .WithSummary("List the validated challenge packages.");

        group.MapGet("/{slug}", GetChallenge)
            .WithName("GetChallenge")
            .WithSummary("Get a challenge statement, starter files, and public test summaries.");

        return endpoints;
    }

    private static Ok<IReadOnlyList<ChallengeSummary>> GetChallenges(
        IChallengeCatalog catalog)
    {
        return TypedResults.Ok(catalog.GetSummaries());
    }

    private static Results<Ok<ChallengeDetail>, ProblemHttpResult> GetChallenge(
        string slug,
        IChallengeCatalog catalog)
    {
        var challenge = catalog.FindBySlug(slug);

        return challenge is null
            ? TypedResults.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Challenge not found.",
                detail: "No challenge matches the requested slug.")
            : TypedResults.Ok(challenge);
    }
}
