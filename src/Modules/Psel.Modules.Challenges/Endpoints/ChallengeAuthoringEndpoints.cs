using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Psel.Modules.Challenges.Contracts;
using Psel.Modules.Challenges.Packages;

namespace Psel.Modules.Challenges.Endpoints;

internal static class ChallengeAuthoringEndpoints
{
    public static IEndpointRouteBuilder MapChallengeAuthoringEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/authoring").WithTags("Authoring");

        group.MapPost("/challenge-packages", ImportChallengePackage)
            .WithName("ImportChallengePackage")
            .WithSummary("Validate and install one uploaded challenge package.")
            .Accepts<IFormFile>("multipart/form-data")
            .Produces<ChallengePackageImportResult>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status413PayloadTooLarge);

        return endpoints;
    }

    private static async Task<Results<Ok<ChallengePackageImportResult>, ProblemHttpResult>>
        ImportChallengePackage(
            HttpContext httpContext,
            ChallengePackageImporter importer,
            CancellationToken cancellationToken)
    {
        if (!httpContext.Request.HasFormContentType)
        {
            return Reject(
                StatusCodes.Status415UnsupportedMediaType,
                "Unsupported content type.",
                "Upload the challenge package as a multipart form with a 'file' field.");
        }

        IFormCollection form;

        try
        {
            form = await httpContext.Request.ReadFormAsync(cancellationToken);
        }
        catch (BadHttpRequestException exception)
        {
            return Reject(exception.StatusCode, "The upload could not be read.", exception.Message);
        }
        catch (InvalidDataException exception)
        {
            return Reject(
                StatusCodes.Status400BadRequest,
                "The upload could not be read.",
                exception.Message);
        }

        var file = form.Files.GetFile("file");

        if (file is null || file.Length == 0)
        {
            return Reject(
                StatusCodes.Status400BadRequest,
                "Challenge package was not provided.",
                "Attach a .zip archive in the form field named 'file'.");
        }

        var replace = bool.TryParse(httpContext.Request.Query["replace"], out var value) && value;

        try
        {
            return TypedResults.Ok(
                await importer.ImportAsync(file, replace, cancellationToken));
        }
        catch (ChallengePackageImportException exception)
        {
            return Reject(
                exception.StatusCode,
                exception.StatusCode == StatusCodes.Status409Conflict
                    ? "Challenge package conflict."
                    : exception.StatusCode == StatusCodes.Status413PayloadTooLarge
                        ? "The challenge package is too large."
                        : "Challenge package rejected.",
                exception.Message);
        }
    }

    private static ProblemHttpResult Reject(int statusCode, string title, string detail)
    {
        return TypedResults.Problem(statusCode: statusCode, title: title, detail: detail);
    }
}
