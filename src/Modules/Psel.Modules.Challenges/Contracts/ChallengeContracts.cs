namespace Psel.Modules.Challenges.Contracts;

/// <summary>
/// Client-facing projections of challenge packages. Hidden tests, reference
/// solutions, and internal package paths are deliberately absent.
/// </summary>
public sealed record ChallengeLanguageSummary(string Id, string Name);

public sealed record ChallengeSummary(
    string Slug,
    string Title,
    string Summary,
    string DomainSlug,
    string Topic,
    string Difficulty,
    string Type,
    int EstimatedMinutes,
    IReadOnlyList<ChallengeLanguageSummary> Languages,
    IReadOnlyList<string> PrerequisiteSlugs,
    IReadOnlyList<string> CompetencySlugs);

public sealed record ChallengeFile(string Path, string Content);

public sealed record ChallengeLanguage(
    string Id,
    string Name,
    string EditorLanguage,
    IReadOnlyList<ChallengeFile> StarterFiles,
    bool CanExecute);

public sealed record ChallengeExample(
    string Input,
    string Output,
    string? Explanation);

public sealed record PublicTestSummary(string Name, string? Description);

public sealed record ChallengeEvaluationSummary(
    string Kind,
    int? TimeLimitSeconds,
    int? MemoryLimitMb);

/// <summary>
/// One server-defined examination variant. The variant and its limit are
/// selected by the server when an examination starts; clients never supply an
/// arbitrary duration or variant identifier.
/// </summary>
public sealed record ExaminationVariantSummary(
    string Id,
    string Label,
    int? TimeLimitSeconds);

public sealed record ChallengeExaminationSummary(
    IReadOnlyList<ExaminationVariantSummary> Variants);

public sealed record ChallengeDetail(
    string Slug,
    string Title,
    string Summary,
    string DomainSlug,
    string Topic,
    string Difficulty,
    string Type,
    int EstimatedMinutes,
    IReadOnlyList<string> Objectives,
    IReadOnlyList<string> PrerequisiteSlugs,
    IReadOnlyList<string> CompetencySlugs,
    IReadOnlyList<string> Constraints,
    IReadOnlyList<ChallengeExample> Examples,
    IReadOnlyList<ChallengeLanguage> Languages,
    IReadOnlyList<PublicTestSummary> PublicTests,
    ChallengeEvaluationSummary Evaluation,
    ChallengeExaminationSummary Examination,
    string StatementMarkdown,
    string? HintsMarkdown);
