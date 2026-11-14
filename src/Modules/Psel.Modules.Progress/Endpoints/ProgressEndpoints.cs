using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Psel.Modules.Progress.Contracts;

namespace Psel.Modules.Progress.Endpoints;

internal static class ProgressEndpoints
{
    public static IEndpointRouteBuilder MapProgressEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api").WithTags("Progress");

        group.MapGet("/progress", GetProgress)
            .WithName("GetProgress")
            .WithSummary("Get evidence-based competency progress derived from assessments.");

        group.MapGet("/assessments", GetAssessments)
            .WithName("ListAssessments")
            .WithSummary("List assessment history, newest first.");

        group.MapPost("/examinations", CreateExamination)
            .WithName("CreateExamination")
            .WithSummary("Start an examination with a server-selected variant and time limit.");

        group.MapGet("/examinations", ListExaminations)
            .WithName("ListExaminations")
            .WithSummary("List examination sessions, newest first.");

        group.MapGet("/examinations/{id:guid}", GetExamination)
            .WithName("GetExamination")
            .WithSummary("Get an examination session and its hint-free challenge content.");

        group.MapPost("/examinations/{id:guid}/finish", FinishExamination)
            .WithName("FinishExamination")
            .WithSummary("Finish an active examination session.");

        return endpoints;
    }

    private static async Task<Ok<ProgressOverview>> GetProgress(
        ProgressService progress,
        CancellationToken cancellationToken)
    {
        return TypedResults.Ok(await progress.GetProgressAsync(cancellationToken));
    }

    private static async Task<Ok<IReadOnlyList<AssessmentHistoryItem>>> GetAssessments(
        int? limit,
        ProgressService progress,
        CancellationToken cancellationToken)
    {
        return TypedResults.Ok(
            await progress.GetAssessmentsAsync(limit ?? 20, cancellationToken));
    }

    private static async Task<Results<Created<ExaminationSessionDetail>, ProblemHttpResult>>
        CreateExamination(
            CreateExaminationRequest request,
            ExaminationService examinations,
            CancellationToken cancellationToken)
    {
        try
        {
            var session = await examinations.CreateAsync(request, cancellationToken);

            return TypedResults.Created($"/api/examinations/{session.Id}", session);
        }
        catch (ExaminationValidationException exception)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Examination rejected.",
                detail: exception.Message);
        }
    }

    private static async Task<Ok<IReadOnlyList<ExaminationSessionSummary>>> ListExaminations(
        int? limit,
        ExaminationService examinations,
        CancellationToken cancellationToken)
    {
        return TypedResults.Ok(await examinations.ListAsync(limit ?? 20, cancellationToken));
    }

    private static async Task<Results<Ok<ExaminationSessionDetail>, ProblemHttpResult>>
        GetExamination(
            Guid id,
            ExaminationService examinations,
            CancellationToken cancellationToken)
    {
        var session = await examinations.FindAsync(id, cancellationToken);

        return session is null
            ? TypedResults.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Examination not found.",
                detail: "No examination matches the requested identifier.")
            : TypedResults.Ok(session);
    }

    private static async Task<Results<Ok<ExaminationSessionDetail>, ProblemHttpResult>>
        FinishExamination(
            Guid id,
            ExaminationService examinations,
            CancellationToken cancellationToken)
    {
        var session = await examinations.FinishAsync(id, cancellationToken);

        return session is null
            ? TypedResults.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Examination not found.",
                detail: "No examination matches the requested identifier.")
            : TypedResults.Ok(session);
    }
}
