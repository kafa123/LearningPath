namespace Psel.Modules.Progress.Contracts;

/// <summary>
/// Cross-module boundary used by the submission module. It only answers whether
/// an attempt belongs to an active examination and records the link; submission
/// details never cross into this module.
/// </summary>
public interface IExaminationPolicy
{
    Task<ExaminationAuthorizationResult> AuthorizeAttemptAsync(
        Guid examinationId,
        string challengeSlug,
        CancellationToken cancellationToken);

    Task RecordAttemptAsync(
        Guid examinationId,
        Guid submissionId,
        CancellationToken cancellationToken);
}

public sealed record ExaminationAuthorizationResult(
    bool IsAllowed,
    string? RejectionReason);
