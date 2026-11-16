using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Psel.Modules.Learning.Authoring;
using Psel.Modules.Learning.Contracts;

namespace Psel.Modules.Learning.Endpoints;

internal static class LearningAuthoringEndpoints
{
    private const long MaxUploadBytes = 2 * 1024 * 1024;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    public static IEndpointRouteBuilder MapLearningAuthoringEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/authoring").WithTags("Authoring");

        group.MapPost("/learning-content", ImportLearningContent)
            .WithName("ImportLearningContent")
            .WithSummary("Upsert learning content from a JSON content pack.")
            .Accepts<LearningContentPack>("application/json", "multipart/form-data")
            .Produces<LearningContentImportResult>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status413PayloadTooLarge);

        return endpoints;
    }

    private static async Task<Results<Ok<LearningContentImportResult>, ProblemHttpResult>>
        ImportLearningContent(
            HttpContext httpContext,
            LearningContentImporter importer,
            CancellationToken cancellationToken)
    {
        LearningContentPack? pack;

        try
        {
            if (httpContext.Request.HasFormContentType)
            {
                var form = await httpContext.Request.ReadFormAsync(cancellationToken);
                var file = form.Files.GetFile("file");

                if (file is null || file.Length == 0)
                {
                    return Reject(
                        StatusCodes.Status400BadRequest,
                        "Learning content was not provided.",
                        "Attach a JSON content pack in the form field named 'file'.");
                }

                if (file.Length > MaxUploadBytes)
                {
                    return TooLarge();
                }

                await using var stream = file.OpenReadStream();
                pack = await JsonSerializer.DeserializeAsync<LearningContentPack>(
                    stream,
                    JsonOptions,
                    cancellationToken);
            }
            else if (httpContext.Request.ContentType?.Contains(
                "application/json",
                StringComparison.OrdinalIgnoreCase) == true)
            {
                if (httpContext.Request.ContentLength > MaxUploadBytes)
                {
                    return TooLarge();
                }

                pack = await JsonSerializer.DeserializeAsync<LearningContentPack>(
                    httpContext.Request.Body,
                    JsonOptions,
                    cancellationToken);
            }
            else
            {
                return Reject(
                    StatusCodes.Status415UnsupportedMediaType,
                    "Unsupported content type.",
                    "Send application/json or a multipart form with a 'file' field.");
            }
        }
        catch (JsonException exception)
        {
            return Reject(
                StatusCodes.Status400BadRequest,
                "The content pack is not valid JSON.",
                exception.Message);
        }
        catch (BadHttpRequestException exception)
        {
            return Reject(
                exception.StatusCode,
                "The content pack could not be read.",
                exception.Message);
        }

        if (pack is null)
        {
            return Reject(
                StatusCodes.Status400BadRequest,
                "The content pack is empty.",
                "The uploaded JSON did not contain a content pack.");
        }

        try
        {
            return TypedResults.Ok(await importer.ImportAsync(pack, cancellationToken));
        }
        catch (LearningContentImportException exception)
        {
            return Reject(
                StatusCodes.Status400BadRequest,
                "Learning content import failed.",
                string.Join(" | ", exception.Errors));
        }
    }

    private static ProblemHttpResult Reject(int statusCode, string title, string detail)
    {
        return TypedResults.Problem(statusCode: statusCode, title: title, detail: detail);
    }

    private static ProblemHttpResult TooLarge()
    {
        return Reject(
            StatusCodes.Status413PayloadTooLarge,
            "The content pack is too large.",
            $"Learning content packs must be at most {MaxUploadBytes / (1024 * 1024)} MiB.");
    }
}
