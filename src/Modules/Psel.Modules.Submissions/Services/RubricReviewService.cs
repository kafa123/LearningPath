using Microsoft.EntityFrameworkCore;
using Psel.Infrastructure.Persistence;
using Psel.Modules.Challenges.Contracts;
using Psel.Modules.Challenges.Packages;
using Psel.Modules.Submissions.Contracts;
using Psel.Modules.Submissions.Domain;

namespace Psel.Modules.Submissions.Services;

internal sealed class RubricReviewNotFoundException(string message) : Exception(message);

internal sealed class RubricReviewValidationException(string message) : Exception(message);

internal sealed class RubricReviewConflictException(string message) : Exception(message);

/// <summary>
/// Persists advisory, learner-authored rubric reviews for completed
/// submissions. Every score and band is derived on the server from the trusted
/// package rubric; clients can only supply one point value per criterion.
/// </summary>
internal sealed class RubricReviewService(
    PselDbContext dbContext,
    IChallengeCatalog challengeCatalog)
{
    private const int MaxNoteLength = 2000;

    public async Task<RubricReviewDetail?> FindAsync(
        Guid submissionId,
        CancellationToken cancellationToken)
    {
        var review = await dbContext
            .Set<RubricReview>()
            .AsNoTracking()
            .Include(candidate => candidate.CriterionScores)
            .FirstOrDefaultAsync(
                candidate => candidate.SubmissionId == submissionId,
                cancellationToken);

        return review is null ? null : ToDetail(review);
    }

    public async Task<RubricReviewDetail> SaveAsync(
        Guid submissionId,
        SaveRubricReviewRequest request,
        CancellationToken cancellationToken)
    {
        var submission = await dbContext
            .Set<Submission>()
            .AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Id == submissionId, cancellationToken);

        if (submission is null)
        {
            throw new RubricReviewNotFoundException(
                "No submission matches the requested identifier.");
        }

        if (!submission.IsTerminal)
        {
            throw new RubricReviewConflictException(
                "Wait for the execution to finish before reviewing it.");
        }

        var challenge = challengeCatalog.FindBySlug(submission.ChallengeSlug);
        var rubric = challenge?.Rubric;

        if (rubric is null || rubric.Criteria.Count == 0)
        {
            throw new RubricReviewValidationException(
                "This challenge does not define a rubric.");
        }

        var points = ValidatePoints(request.Criteria, rubric);
        var note = ValidateNote(request.Note);
        var score = ComputeScore(rubric, points);
        var band = BandFor(score);
        var now = DateTimeOffset.UtcNow;

        var review = await dbContext
            .Set<RubricReview>()
            .Include(candidate => candidate.CriterionScores)
            .FirstOrDefaultAsync(
                candidate => candidate.SubmissionId == submissionId,
                cancellationToken);

        if (review is null)
        {
            review = RubricReview.Create(
                submissionId,
                submission.ChallengeSlug,
                score,
                band,
                note,
                now);
            dbContext.Set<RubricReview>().Add(review);
        }
        else
        {
            review.Update(score, band, note, now);
        }

        SynchronizeScores(review, rubric, points);

        await dbContext.SaveChangesAsync(cancellationToken);

        return ToDetail(review);
    }

    private static Dictionary<string, int> ValidatePoints(
        IReadOnlyList<RubricCriterionInput>? inputs,
        ChallengeRubric rubric)
    {
        if (inputs is null || inputs.Count != rubric.Criteria.Count)
        {
            throw new RubricReviewValidationException(
                "Submit exactly one score for every rubric criterion.");
        }

        var criteria = rubric.Criteria.ToDictionary(
            criterion => criterion.Id,
            StringComparer.OrdinalIgnoreCase);
        var points = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var input in inputs)
        {
            var criterionId = input?.Id?.Trim();

            if (string.IsNullOrWhiteSpace(criterionId)
                || !criteria.TryGetValue(criterionId, out var criterion))
            {
                throw new RubricReviewValidationException(
                    "A rubric score refers to an unknown criterion.");
            }

            if (input?.Points is null)
            {
                throw new RubricReviewValidationException(
                    $"Criterion '{criterion.Id}' needs a point value.");
            }

            if (input.Points < 0 || input.Points > criterion.MaxPoints)
            {
                throw new RubricReviewValidationException(
                    $"Criterion '{criterion.Id}' points must be between 0 and {criterion.MaxPoints}.");
            }

            if (!points.TryAdd(criterion.Id, input.Points.Value))
            {
                throw new RubricReviewValidationException(
                    $"Criterion '{criterion.Id}' is scored more than once.");
            }
        }

        if (points.Count != rubric.Criteria.Count)
        {
            throw new RubricReviewValidationException(
                "Submit exactly one score for every rubric criterion.");
        }

        return points;
    }

    private static string? ValidateNote(string? note)
    {
        if (string.IsNullOrWhiteSpace(note))
        {
            return null;
        }

        var trimmed = note.Trim();

        if (trimmed.Length > MaxNoteLength)
        {
            throw new RubricReviewValidationException(
                $"The review note may contain at most {MaxNoteLength} characters.");
        }

        return trimmed;
    }

    private static int ComputeScore(
        ChallengeRubric rubric,
        IReadOnlyDictionary<string, int> points)
    {
        var weightedMaximum = rubric.Criteria.Sum(
            criterion => criterion.MaxPoints * criterion.Weight);
        var weightedPoints = rubric.Criteria.Sum(
            criterion => points[criterion.Id] * criterion.Weight);

        if (weightedMaximum <= 0)
        {
            return 0;
        }

        return (int)Math.Round(
            weightedPoints * 100d / weightedMaximum,
            MidpointRounding.AwayFromZero);
    }

    private static string BandFor(int score)
    {
        return score switch
        {
            >= 85 => "Exemplary",
            >= 70 => "Proficient",
            >= 50 => "Developing",
            _ => "Beginning"
        };
    }

    private static void SynchronizeScores(
        RubricReview review,
        ChallengeRubric rubric,
        IReadOnlyDictionary<string, int> points)
    {
        var existing = review.CriterionScores.ToDictionary(
            score => score.CriterionId,
            StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < rubric.Criteria.Count; index++)
        {
            var criterion = rubric.Criteria[index];

            if (existing.Remove(criterion.Id, out var score))
            {
                score.Update(
                    criterion.Title,
                    points[criterion.Id],
                    criterion.MaxPoints,
                    criterion.Weight,
                    index);
                continue;
            }

            review.CriterionScores.Add(RubricCriterionScore.Create(
                criterion.Id,
                criterion.Title,
                points[criterion.Id],
                criterion.MaxPoints,
                criterion.Weight,
                index));
        }

        foreach (var stale in existing.Values)
        {
            review.CriterionScores.Remove(stale);
        }
    }

    private RubricReviewDetail ToDetail(RubricReview review)
    {
        var rubric = challengeCatalog.FindBySlug(review.ChallengeSlug)?.Rubric;
        var descriptions = rubric?.Criteria.ToDictionary(
            criterion => criterion.Id,
            criterion => criterion.Description,
            StringComparer.OrdinalIgnoreCase);

        var criteria = review.CriterionScores
            .OrderBy(score => score.SortOrder)
            .Select(score => new RubricCriterionReview(
                score.CriterionId,
                score.Title,
                descriptions is not null
                    && descriptions.TryGetValue(score.CriterionId, out var description)
                        ? description
                        : null,
                score.MaxPoints,
                score.Weight,
                score.Points))
            .ToList();

        return new RubricReviewDetail(
            review.SubmissionId,
            review.ChallengeSlug,
            review.Score,
            review.Band,
            review.Note,
            criteria,
            review.CreatedAtUtc,
            review.UpdatedAtUtc);
    }
}
