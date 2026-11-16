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

        group.MapGet("/{id:guid}/rubric", GetRubricReview)
            .WithName("GetRubricReview")
            .WithSummary("Get the advisory rubric review for one submission, when present.");

        group.MapPut("/{id:guid}/rubric", SaveRubricReview)
            .WithName("SaveRubricReview")
            .WithSummary("Create or replace the advisory rubric self-review for a completed submission.");

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

    private static async Task<Results<Ok<RubricReviewDetail>, ProblemHttpResult>> GetRubricReview(
        Guid id,
        RubricReviewService reviews,
        CancellationToken cancellationToken)
    {
        var review = await reviews.FindAsync(id, cancellationToken);

        return review is null
            ? TypedResults.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Rubric review not found.",
                detail: "This submission has not been reviewed against its rubric yet.")
            : TypedResults.Ok(review);
    }

    private static async Task<Results<Ok<RubricReviewDetail>, ProblemHttpResult>> SaveRubricReview(
        Guid id,
        SaveRubricReviewRequest request,
        RubricReviewService reviews,
        CancellationToken cancellationToken)
    {
        try
        {
            return TypedResults.Ok(await reviews.SaveAsync(id, request, cancellationToken));
        }
        catch (RubricReviewNotFoundException exception)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Submission not found.",
                detail: exception.Message);
        }
        catch (RubricReviewValidationException exception)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Rubric review rejected.",
                detail: exception.Message);
        }
        catch (RubricReviewConflictException exception)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Rubric review not available yet.",
                detail: exception.Message);
        }
    }
}
