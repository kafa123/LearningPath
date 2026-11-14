using Microsoft.EntityFrameworkCore;
using Psel.Infrastructure.Persistence;
using Psel.Modules.Assessment.Contracts;
using Psel.Modules.Assessment.Domain;
using Psel.Modules.Assessment.Services;
using AssessmentEntity = Psel.Modules.Assessment.Domain.Assessment;

namespace Psel.Modules.Assessment;

/// <summary>
/// Application boundary for deterministic grading and read-only result views.
/// Callers persist the returned entity in the shared transaction boundary.
/// </summary>
public sealed class AssessmentService(PselDbContext dbContext)
{
    public AssessmentGrade Record(AssessmentInput input)
    {
        var grade = AssessmentGrader.Grade(input);
        dbContext.Set<AssessmentEntity>().Add(grade.Assessment);

        return grade;
    }

    public async Task<AssessmentResultDetail?> FindResultAsync(
        Guid submissionId,
        CancellationToken cancellationToken)
    {
        var assessment = await dbContext
            .Set<AssessmentEntity>()
            .AsNoTracking()
            .Include(candidate => candidate.TestResults)
            .FirstOrDefaultAsync(
                candidate => candidate.SubmissionId == submissionId,
                cancellationToken);

        if (assessment is null)
        {
            return null;
        }

        var publicTests = assessment.TestResults
            .Where(result => result.Visibility == AssessmentVisibility.Public)
            .OrderBy(result => result.SortOrder)
            .Select(result => new AssessmentTestResultView(
                result.Name,
                result.Description,
                result.Status.ToString()))
            .ToList();

        return new AssessmentResultDetail(
            assessment.SubmissionId,
            assessment.ChallengeSlug,
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
            publicTests,
            assessment.CreatedAtUtc);
    }
}
