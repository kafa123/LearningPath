using Microsoft.EntityFrameworkCore;
using Psel.Infrastructure.Persistence;
using Psel.Modules.Challenges.Packages;
using Psel.Modules.Progress.Contracts;
using Psel.Modules.Progress.Domain;
using ExaminationSessionEntity = Psel.Modules.Progress.Domain.ExaminationSession;

namespace Psel.Modules.Progress;

internal sealed class ExaminationValidationException(string message) : Exception(message);

/// <summary>
/// Creates and advances examination sessions. The server owns variant selection,
/// time limits, hidden-test selection, and the hint-free projection; clients only
/// provide the challenge slug and their source files.
/// </summary>
public sealed class ExaminationService(
    PselDbContext dbContext,
    IChallengeCatalog challengeCatalog,
    IChallengeExecutionSource executionSource,
    TimeProvider timeProvider) : IExaminationPolicy
{
    public async Task<ExaminationSessionDetail> CreateAsync(
        CreateExaminationRequest request,
        CancellationToken cancellationToken)
    {
        var challengeSlug = request.ChallengeSlug?.Trim();
        var challenge = string.IsNullOrWhiteSpace(challengeSlug)
            ? null
            : challengeCatalog.FindBySlug(challengeSlug);

        if (challenge is null)
        {
            throw new ExaminationValidationException("The challenge does not exist.");
        }

        if (!challenge.Languages.Any(language => language.CanExecute))
        {
            throw new ExaminationValidationException(
                "This challenge cannot be executed and cannot be used for an examination.");
        }

        var testPlan = executionSource.GetTestPlan(challenge.Slug);

        if (testPlan is null
            || !testPlan.Tests.Any(test => string.Equals(
                test.Visibility,
                "hidden",
                StringComparison.OrdinalIgnoreCase)))
        {
            throw new ExaminationValidationException(
                "This challenge does not define hidden assessment tests.");
        }

        var variants = challenge.Examination.Variants;

        if (variants.Count == 0)
        {
            throw new ExaminationValidationException(
                "This challenge has no examination variants.");
        }

        var variant = variants[Random.Shared.Next(variants.Count)];
        var session = ExaminationSessionEntity.Start(
            challenge.Slug,
            variant.Id,
            variant.Label,
            variant.TimeLimitSeconds,
            timeProvider.GetUtcNow());

        dbContext.Set<ExaminationSessionEntity>().Add(session);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToDetail(session, challenge, timeProvider.GetUtcNow());
    }

    public async Task<ExaminationSessionDetail?> FindAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var session = await FindSessionAsync(id, cancellationToken);

        if (session is null)
        {
            return null;
        }

        await RefreshExpiryAsync(session, cancellationToken);

        var challenge = challengeCatalog.FindBySlug(session.ChallengeSlug);

        return challenge is null
            ? null
            : ToDetail(session, challenge, timeProvider.GetUtcNow());
    }

    public async Task<ExaminationSessionDetail?> FinishAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var session = await FindSessionAsync(id, cancellationToken);

        if (session is null)
        {
            return null;
        }

        var now = timeProvider.GetUtcNow();
        await RefreshExpiryAsync(session, cancellationToken);

        if (session.Status == ExaminationStatus.InProgress)
        {
            session.Complete(now);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var challenge = challengeCatalog.FindBySlug(session.ChallengeSlug);

        return challenge is null
            ? null
            : ToDetail(session, challenge, now);
    }

    public async Task<IReadOnlyList<ExaminationSessionSummary>> ListAsync(
        int limit,
        CancellationToken cancellationToken)
    {
        var boundedLimit = Math.Clamp(limit, 1, 50);

        // SQLite cannot ORDER BY a DateTimeOffset column, so the newest window
        // is selected in memory. Sessions are few in this local-first app.
        var sessions = await dbContext
            .Set<ExaminationSessionEntity>()
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        sessions = sessions
            .OrderByDescending(session => session.StartedAtUtc)
            .Take(boundedLimit)
            .ToList();

        if (sessions.Count == 0)
        {
            return [];
        }

        var ids = sessions.Select(session => session.Id).ToList();
        var counts = await dbContext
            .Set<ExaminationAttempt>()
            .AsNoTracking()
            .Where(attempt => ids.Contains(attempt.ExaminationId))
            .GroupBy(attempt => attempt.ExaminationId)
            .Select(group => new { ExaminationId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.ExaminationId, item => item.Count, cancellationToken);

        var now = timeProvider.GetUtcNow();
        var summaries = new List<ExaminationSessionSummary>(sessions.Count);

        foreach (var session in sessions)
        {
            var challenge = challengeCatalog.FindBySlug(session.ChallengeSlug);

            if (challenge is null)
            {
                continue;
            }

            summaries.Add(ToSummary(
                session,
                challenge.Title,
                counts.GetValueOrDefault(session.Id),
                now));
        }

        return summaries;
    }

    public async Task<ExaminationAuthorizationResult> AuthorizeAttemptAsync(
        Guid examinationId,
        string challengeSlug,
        CancellationToken cancellationToken)
    {
        var session = await FindSessionAsync(examinationId, cancellationToken);

        if (session is null)
        {
            return new ExaminationAuthorizationResult(
                false,
                "The examination does not exist.");
        }

        await RefreshExpiryAsync(session, cancellationToken);

        if (session.Status == ExaminationStatus.Expired)
        {
            return new ExaminationAuthorizationResult(
                false,
                "The examination time limit has expired.");
        }

        if (session.Status != ExaminationStatus.InProgress)
        {
            return new ExaminationAuthorizationResult(
                false,
                "The examination is no longer active.");
        }

        if (!string.Equals(
            session.ChallengeSlug,
            challengeSlug,
            StringComparison.OrdinalIgnoreCase))
        {
            return new ExaminationAuthorizationResult(
                false,
                "The submission challenge does not match the examination.");
        }

        return new ExaminationAuthorizationResult(true, null);
    }

    public async Task RecordAttemptAsync(
        Guid examinationId,
        Guid submissionId,
        CancellationToken cancellationToken)
    {
        dbContext.Set<ExaminationAttempt>().Add(
            ExaminationAttempt.Create(examinationId, submissionId, timeProvider.GetUtcNow()));

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<ExaminationSessionEntity?> FindSessionAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        return await dbContext
            .Set<ExaminationSessionEntity>()
            .FirstOrDefaultAsync(session => session.Id == id, cancellationToken);
    }

    private async Task RefreshExpiryAsync(
        ExaminationSessionEntity session,
        CancellationToken cancellationToken)
    {
        if (session.HasExpired(timeProvider.GetUtcNow()))
        {
            session.Expire();
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private ExaminationSessionDetail ToDetail(
        ExaminationSessionEntity session,
        Psel.Modules.Challenges.Contracts.ChallengeDetail challenge,
        DateTimeOffset now)
    {
        var summary = ToSummary(session, challenge.Title, 0, now);

        return new ExaminationSessionDetail(
            summary.Id,
            summary.ChallengeSlug,
            summary.ChallengeTitle,
            summary.VariantId,
            summary.VariantLabel,
            summary.TimeLimitSeconds,
            summary.Status,
            summary.IsActive,
            summary.RemainingSeconds,
            summary.StartedAtUtc,
            summary.ExpiresAtUtc,
            summary.CompletedAtUtc,
            challenge with { HintsMarkdown = null });
    }

    private static ExaminationSessionSummary ToSummary(
        ExaminationSessionEntity session,
        string challengeTitle,
        int attemptCount,
        DateTimeOffset now)
    {
        var isActive = session.Status == ExaminationStatus.InProgress;
        int? remainingSeconds = session.ExpiresAtUtc is { } expires
            ? Math.Max(0, (int)Math.Ceiling((expires - now).TotalSeconds))
            : null;

        return new ExaminationSessionSummary(
            session.Id,
            session.ChallengeSlug,
            challengeTitle,
            session.VariantId,
            session.VariantLabel,
            session.TimeLimitSeconds,
            session.Status.ToString(),
            isActive,
            remainingSeconds,
            session.StartedAtUtc,
            session.ExpiresAtUtc,
            session.CompletedAtUtc,
            attemptCount);
    }
}
