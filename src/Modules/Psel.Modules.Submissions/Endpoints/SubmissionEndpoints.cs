using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Psel.Modules.Submissions.Contracts;
using Psel.Modules.Submissions.Services;

namespace Psel.Modules.Submissions.Endpoints;

internal static class SubmissionEndpoints
{
    public static IEndpointRouteBuilder MapSubmissionEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/submissions").WithTags("Submissions");

        group.MapPost("", CreateSubmission)
            .WithName("CreateSubmission")
            .WithSummary("Queue one execution attempt for a challenge language.");

        group.MapGet("/{id:guid}", GetSubmission)
            .WithName("GetSubmission")
            .WithSummary("Get the status, diagnostics, and output of one execution attempt.");

        return endpoints;
    }

    private static async Task<Results<Accepted<SubmissionCreated>, ProblemHttpResult>> CreateSubmission(
        CreateSubmissionRequest request,
        SubmissionService submissions,
        CancellationToken cancellationToken)
    {
        try
        {
            var created = await submissions.CreateAsync(request, cancellationToken);

            return TypedResults.Accepted($"/api/submissions/{created.Id}", created);
        }
        catch (SubmissionValidationException exception)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Submission rejected.",
                detail: exception.Message);
        }
        catch (SubmissionQueueUnavailableException exception)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Execution queue unavailable.",
                detail: exception.Message);
        }
    }

    private static async Task<Results<Ok<SubmissionDetail>, ProblemHttpResult>> GetSubmission(
        Guid id,
        SubmissionService submissions,
        CancellationToken cancellationToken)
    {
        var submission = await submissions.FindAsync(id, cancellationToken);

        return submission is null
            ? TypedResults.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Submission not found.",
                detail: "No submission matches the requested identifier.")
            : TypedResults.Ok(submission);
    }
}
