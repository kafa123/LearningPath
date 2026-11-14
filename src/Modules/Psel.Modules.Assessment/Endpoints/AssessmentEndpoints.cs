using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Psel.Modules.Assessment.Contracts;

namespace Psel.Modules.Assessment.Endpoints;

internal static class AssessmentEndpoints
{
    public static IEndpointRouteBuilder MapAssessmentEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints
            .MapGet("/api/submissions/{id:guid}/results", GetResult)
            .WithTags("Assessments")
            .WithName("GetSubmissionResult")
            .WithSummary("Get the deterministic assessment for one submission attempt.");

        return endpoints;
    }

    private static async Task<Results<Ok<AssessmentResultDetail>, ProblemHttpResult>> GetResult(
        Guid id,
        AssessmentService assessments,
        CancellationToken cancellationToken)
    {
        var result = await assessments.FindResultAsync(id, cancellationToken);

        return result is null
            ? TypedResults.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Assessment result not found.",
                detail: "No assessment result is available for this submission.")
            : TypedResults.Ok(result);
    }
}
