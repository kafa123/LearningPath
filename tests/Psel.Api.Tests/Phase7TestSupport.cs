using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Psel.Api.Tests;

internal static class Phase7TestSupport
{
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static async Task<SubmissionCreatedDto> PostSubmissionAsync(
        HttpClient client,
        string challengeSlug,
        string purpose,
        Guid? examinationId,
        CancellationToken cancellationToken)
    {
        using var response = await client.PostAsJsonAsync(
            "/api/submissions",
            new
            {
                challengeSlug,
                languageId = "csharp",
                purpose,
                examinationId,
                files = new[] { new { path = "Solution.cs", content = "// code" } }
            },
            cancellationToken);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<SubmissionCreatedDto>(
            JsonOptions,
            cancellationToken);

        Assert.NotNull(created);
        return created;
    }

    public static async Task<SubmissionDetailDto> WaitForTerminalAsync(
        HttpClient client,
        Guid id,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 100; attempt++)
        {
            var detail = await client.GetFromJsonAsync<SubmissionDetailDto>(
                $"/api/submissions/{id}",
                JsonOptions,
                cancellationToken);

            if (detail is not null && detail.IsTerminal)
            {
                return detail;
            }

            await Task.Delay(50, cancellationToken);
        }

        throw new InvalidOperationException(
            $"Submission {id} did not reach a terminal state in time.");
    }

    public static async Task<ExaminationDetailDto> CreateExaminationAsync(
        HttpClient client,
        string challengeSlug,
        CancellationToken cancellationToken)
    {
        using var response = await client.PostAsJsonAsync(
            "/api/examinations",
            new { challengeSlug },
            cancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var detail = await response.Content.ReadFromJsonAsync<ExaminationDetailDto>(
            JsonOptions,
            cancellationToken);

        Assert.NotNull(detail);
        return detail;
    }

    public static CompetencyProgressDto FindCompetency(
        ProgressOverviewDto overview,
        string slug)
    {
        return overview.Domains
            .SelectMany(domain => domain.Modules)
            .SelectMany(module => module.Competencies)
            .Single(competency => competency.Slug == slug);
    }
}

internal sealed record SubmissionCreatedDto(
    Guid Id,
    string Status,
    DateTimeOffset CreatedAtUtc);

internal sealed record SubmissionDetailDto(
    Guid Id,
    string ChallengeSlug,
    string LanguageId,
    string Status,
    bool IsTerminal,
    string Purpose);

internal sealed record ProgressEvidenceDto(
    string ChallengeSlug,
    string ChallengeTitle,
    string Purpose,
    string Outcome,
    int Score,
    int PassedTests,
    int TotalTests,
    bool IsExamination,
    DateTimeOffset CreatedAtUtc);

internal sealed record CompetencyProgressDto(
    string Slug,
    string Name,
    string Description,
    string State,
    string NextAction,
    List<ProgressEvidenceDto> Evidence);

internal sealed record ModuleProgressDto(
    string Slug,
    string Title,
    string Level,
    int SortOrder,
    List<CompetencyProgressDto> Competencies);

internal sealed record DomainProgressDto(
    string Slug,
    string Name,
    string Description,
    int SortOrder,
    List<ModuleProgressDto> Modules);

internal sealed record ProgressSummaryDto(
    int Total,
    int NotStarted,
    int Learning,
    int Practicing,
    int AssessmentRequired,
    int Mastered);

internal sealed record ProgressOverviewDto(
    ProgressSummaryDto Summary,
    List<DomainProgressDto> Domains);

internal sealed record AssessmentHistoryItemDto(
    Guid SubmissionId,
    string ChallengeSlug,
    string ChallengeTitle,
    string LanguageId,
    string Purpose,
    string Outcome,
    bool IsAccepted,
    int Score,
    int PassedTests,
    int TotalTests,
    int PublicPassed,
    int PublicTotal,
    int HiddenPassed,
    int HiddenTotal,
    bool IsExamination,
    Guid? ExaminationId,
    string? ExaminationVariant,
    DateTimeOffset CreatedAtUtc);

internal sealed record ExaminationChallengeDto(
    string Slug,
    string Title,
    string? HintsMarkdown);

internal sealed record ExaminationDetailDto(
    Guid Id,
    string ChallengeSlug,
    string ChallengeTitle,
    string VariantId,
    string VariantLabel,
    int? TimeLimitSeconds,
    string Status,
    bool IsActive,
    int? RemainingSeconds,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? ExpiresAtUtc,
    DateTimeOffset? CompletedAtUtc,
    ExaminationChallengeDto Challenge);

internal sealed record ExaminationSummaryDto(
    Guid Id,
    string ChallengeSlug,
    string ChallengeTitle,
    string VariantId,
    string VariantLabel,
    int? TimeLimitSeconds,
    string Status,
    bool IsActive,
    int? RemainingSeconds,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? ExpiresAtUtc,
    DateTimeOffset? CompletedAtUtc,
    int AttemptCount);
