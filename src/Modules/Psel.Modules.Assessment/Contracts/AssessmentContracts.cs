using Psel.Modules.Execution;

namespace Psel.Modules.Assessment.Contracts;

/// <summary>
/// One test selected by the server for an assessment. Only public tests are
/// included in the client-facing result.
/// </summary>
public sealed record AssessmentTestDefinition(
    string Id,
    bool IsPublic,
    string Name,
    string? Description);

public sealed record AssessmentInput(
    Guid SubmissionId,
    string ChallengeSlug,
    string LanguageId,
    string Purpose,
    ExecutionStatus ExecutionStatus,
    string StandardOutput,
    IReadOnlyList<AssessmentTestDefinition> Tests);

public sealed record AssessmentTestResultView(
    string Name,
    string? Description,
    string Status);

public sealed record AssessmentResultDetail(
    Guid SubmissionId,
    string ChallengeSlug,
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
    IReadOnlyList<AssessmentTestResultView> PublicTests,
    DateTimeOffset CreatedAtUtc);
