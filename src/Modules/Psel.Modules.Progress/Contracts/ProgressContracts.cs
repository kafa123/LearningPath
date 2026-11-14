using Psel.Modules.Challenges.Contracts;

namespace Psel.Modules.Progress.Contracts;

public sealed record CreateExaminationRequest(string? ChallengeSlug);

public sealed record ExaminationSessionSummary(
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

public sealed record ExaminationSessionDetail(
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
    ChallengeDetail Challenge);

public sealed record CompetencyEvidence(
    string ChallengeSlug,
    string ChallengeTitle,
    string Purpose,
    string Outcome,
    int Score,
    int PassedTests,
    int TotalTests,
    bool IsExamination,
    DateTimeOffset CreatedAtUtc);

public sealed record CompetencyProgress(
    string Slug,
    string Name,
    string Description,
    string State,
    string NextAction,
    IReadOnlyList<CompetencyEvidence> Evidence);

public sealed record ModuleProgress(
    string Slug,
    string Title,
    string Level,
    int SortOrder,
    IReadOnlyList<CompetencyProgress> Competencies);

public sealed record DomainProgress(
    string Slug,
    string Name,
    string Description,
    int SortOrder,
    IReadOnlyList<ModuleProgress> Modules);

public sealed record ProgressSummary(
    int Total,
    int NotStarted,
    int Learning,
    int Practicing,
    int AssessmentRequired,
    int Mastered);

public sealed record ProgressOverview(
    ProgressSummary Summary,
    IReadOnlyList<DomainProgress> Domains);

public sealed record AssessmentHistoryItem(
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
