namespace Psel.Modules.Challenges.Contracts;

/// <summary>
/// Result of uploading one challenge package. Filesystem paths are deliberately
/// not part of the response.
/// </summary>
public sealed record ChallengePackageImportResult(
    string Slug,
    string Title,
    string DomainSlug,
    int ChallengeCount,
    bool Replaced);
