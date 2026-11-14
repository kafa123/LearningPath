using Microsoft.EntityFrameworkCore;
using Psel.Infrastructure.Persistence;
using Psel.Modules.Assessment.Domain;
using Psel.Modules.Challenges.Packages;
using Psel.Modules.Learning.Domain;
using Psel.Modules.Progress.Contracts;
using Psel.Modules.Progress.Domain;
using AssessmentEntity = Psel.Modules.Assessment.Domain.Assessment;

namespace Psel.Modules.Progress;

/// <summary>
/// Read-side aggregation over learning content, challenge metadata, and
/// append-only assessment evidence. Progress is derived on read so it cannot
/// drift from the persisted assessment history.
/// </summary>
public sealed class ProgressService(
    PselDbContext dbContext,
    IChallengeCatalog challengeCatalog)
{
    public async Task<ProgressOverview> GetProgressAsync(CancellationToken cancellationToken)
    {
        var domains = await dbContext
            .Set<LearningDomain>()
            .AsNoTracking()
            .AsSplitQuery()
            .OrderBy(domain => domain.SortOrder)
            .Select(domain => new
            {
                domain.Slug,
                domain.Name,
                domain.Description,
                domain.SortOrder,
                Modules = domain.Modules
                    .OrderBy(module => module.SortOrder)
                    .Select(module => new
                    {
                        module.Slug,
                        module.Title,
                        module.Level,
                        module.SortOrder,
                        Competencies = module.Competencies
                            .OrderBy(competency => competency.SortOrder)
                            .Select(competency => new
                            {
                                competency.Slug,
                                competency.Name,
                                competency.Description,
                                competency.SortOrder
                            })
                            .ToList()
                    })
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        var assessmentRows = await dbContext
            .Set<AssessmentEntity>()
            .AsNoTracking()
            .Select(assessment => new
            {
                assessment.SubmissionId,
                assessment.ChallengeSlug,
                assessment.Purpose,
                assessment.Outcome,
                assessment.IsAccepted,
                assessment.Score,
                assessment.PassedTests,
                assessment.TotalTests,
                assessment.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        var assessments = assessmentRows
            .Select(assessment => new AssessmentEvidence(
                assessment.SubmissionId,
                assessment.ChallengeSlug,
                assessment.Purpose,
                assessment.Outcome.ToString(),
                assessment.IsAccepted,
                assessment.Score,
                assessment.PassedTests,
                assessment.TotalTests,
                assessment.CreatedAtUtc))
            .ToList();

        var examinationAttempts = await dbContext
            .Set<ExaminationAttempt>()
            .AsNoTracking()
            .Select(attempt => new ExaminationEvidence(
                attempt.SubmissionId,
                attempt.ExaminationId,
                attempt.Examination!.VariantLabel))
            .ToListAsync(cancellationToken);

        var challenges = challengeCatalog.GetSummaries();
        var challengeBySlug = challenges.ToDictionary(
            challenge => challenge.Slug,
            StringComparer.OrdinalIgnoreCase);
        var examinationBySubmission = examinationAttempts.ToDictionary(
            attempt => attempt.SubmissionId);
        var assessmentsByChallenge = assessments
            .GroupBy(
                assessment => assessment.ChallengeSlug,
                StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.OrderByDescending(item => item.CreatedAtUtc).ToList(),
                StringComparer.OrdinalIgnoreCase);

        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        var domainProgress = new List<DomainProgress>(domains.Count);

        foreach (var domain in domains)
        {
            var modules = new List<ModuleProgress>(domain.Modules.Count);

            foreach (var module in domain.Modules)
            {
                var competencies = new List<CompetencyProgress>(module.Competencies.Count);

                foreach (var competency in module.Competencies)
                {
                    var mappedChallenges = challenges
                        .Where(challenge => challenge.CompetencySlugs.Any(slug =>
                            string.Equals(slug, competency.Slug, StringComparison.OrdinalIgnoreCase)))
                        .ToList();

                    var evidence = mappedChallenges
                        .SelectMany(challenge =>
                            assessmentsByChallenge.GetValueOrDefault(challenge.Slug)
                            ?? [])
                        .OrderByDescending(item => item.CreatedAtUtc)
                        .ToList();

                    var submitEvidence = evidence
                        .Where(item => string.Equals(
                            item.Purpose,
                            "Submit",
                            StringComparison.OrdinalIgnoreCase))
                        .ToList();
                    var acceptedSubmit = submitEvidence
                        .Where(item => item.IsAccepted)
                        .ToList();
                    var acceptedChallenges = acceptedSubmit
                        .Select(item => item.ChallengeSlug)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .Count();
                    var hasAcceptedExamination = acceptedSubmit.Any(item =>
                        examinationBySubmission.ContainsKey(item.SubmissionId));

                    var state = DetermineState(
                        evidence.Count,
                        submitEvidence.Count,
                        acceptedSubmit.Count,
                        acceptedChallenges,
                        hasAcceptedExamination);

                    counts[state] = counts.GetValueOrDefault(state) + 1;

                    competencies.Add(new CompetencyProgress(
                        competency.Slug,
                        competency.Name,
                        competency.Description,
                        state,
                        NextAction(state, acceptedChallenges, hasAcceptedExamination),
                        evidence
                            .Take(5)
                            .Select(item => new CompetencyEvidence(
                                item.ChallengeSlug,
                                challengeBySlug.TryGetValue(item.ChallengeSlug, out var challenge)
                                    ? challenge.Title
                                    : item.ChallengeSlug,
                                item.Purpose,
                                item.Outcome,
                                item.Score,
                                item.PassedTests,
                                item.TotalTests,
                                examinationBySubmission.ContainsKey(item.SubmissionId),
                                item.CreatedAtUtc))
                            .ToList()));
                }

                modules.Add(new ModuleProgress(
                    module.Slug,
                    module.Title,
                    module.Level,
                    module.SortOrder,
                    competencies));
            }

            domainProgress.Add(new DomainProgress(
                domain.Slug,
                domain.Name,
                domain.Description,
                domain.SortOrder,
                modules));
        }

        var total = counts.Values.Sum();

        return new ProgressOverview(
            new ProgressSummary(
                total,
                counts.GetValueOrDefault("NotStarted"),
                counts.GetValueOrDefault("Learning"),
                counts.GetValueOrDefault("Practicing"),
                counts.GetValueOrDefault("AssessmentRequired"),
                counts.GetValueOrDefault("Mastered")),
            domainProgress);
    }

    public async Task<IReadOnlyList<AssessmentHistoryItem>> GetAssessmentsAsync(
        int limit,
        CancellationToken cancellationToken)
    {
        var boundedLimit = Math.Clamp(limit, 1, 100);

        // SQLite cannot ORDER BY a DateTimeOffset column, so the newest window
        // is selected in memory. The local assessment history is small enough
        // that this remains cheap, and it keeps ordering deterministic.
        var assessments = await dbContext
            .Set<AssessmentEntity>()
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        assessments = assessments
            .OrderByDescending(assessment => assessment.CreatedAtUtc)
            .Take(boundedLimit)
            .ToList();

        if (assessments.Count == 0)
        {
            return [];
        }

        var submissionIds = assessments.Select(assessment => assessment.SubmissionId).ToList();
        var attempts = await dbContext
            .Set<ExaminationAttempt>()
            .AsNoTracking()
            .Where(attempt => submissionIds.Contains(attempt.SubmissionId))
            .Select(attempt => new ExaminationEvidence(
                attempt.SubmissionId,
                attempt.ExaminationId,
                attempt.Examination!.VariantLabel))
            .ToDictionaryAsync(attempt => attempt.SubmissionId, cancellationToken);

        return assessments
            .Select(assessment =>
            {
                attempts.TryGetValue(assessment.SubmissionId, out var examination);
                var challenge = challengeCatalog.FindBySlug(assessment.ChallengeSlug);

                return new AssessmentHistoryItem(
                    assessment.SubmissionId,
                    assessment.ChallengeSlug,
                    challenge?.Title ?? assessment.ChallengeSlug,
                    assessment.LanguageId,
                    assessment.Purpose,
                    assessment.Outcome.ToString(),
                    assessment.IsAccepted,
                    assessment.Score,
                    assessment.PassedTests,
                    assessment.TotalTests,
                    assessment.PublicPassed,
                    assessment.PublicTotal,
                    assessment.HiddenPassed,
                    assessment.HiddenTotal,
                    examination is not null,
                    examination?.ExaminationId,
                    examination?.VariantLabel,
                    assessment.CreatedAtUtc);
            })
            .ToList();
    }

    private static string DetermineState(
        int evidenceCount,
        int submitCount,
        int acceptedSubmitCount,
        int acceptedChallenges,
        bool hasAcceptedExamination)
    {
        if (evidenceCount == 0)
        {
            return "NotStarted";
        }

        if (submitCount == 0)
        {
            return "Learning";
        }

        if (acceptedSubmitCount == 0)
        {
            return "Practicing";
        }

        if (hasAcceptedExamination && acceptedChallenges >= 2)
        {
            return "Mastered";
        }

        return "AssessmentRequired";
    }

    private static string NextAction(
        string state,
        int acceptedChallenges,
        bool hasAcceptedExamination)
    {
        return state switch
        {
            "NotStarted" => "Open a mapped challenge and run its public tests.",
            "Learning" => "Submit a mapped challenge to run the hidden assessment tests.",
            "Practicing" => "Pass every hidden test on a mapped challenge.",
            "AssessmentRequired" when !hasAcceptedExamination =>
                "Take an examination for a mapped challenge.",
            "AssessmentRequired" when acceptedChallenges < 2 =>
                "Pass a submission assessment on a second mapped challenge.",
            _ => "Keep the competency current with another challenge."
        };
    }

    private sealed record AssessmentEvidence(
        Guid SubmissionId,
        string ChallengeSlug,
        string Purpose,
        string Outcome,
        bool IsAccepted,
        int Score,
        int PassedTests,
        int TotalTests,
        DateTimeOffset CreatedAtUtc);

    private sealed record ExaminationEvidence(
        Guid SubmissionId,
        Guid ExaminationId,
        string VariantLabel);
}
