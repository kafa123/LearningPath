namespace Psel.Modules.Challenges.Packages;

/// <summary>
/// Resolved location of the trusted challenge package directory.
/// </summary>
public sealed record ChallengeLocation(string Path);

public static class ChallengePathResolver
{
    public static string Resolve(string? configuredPath, string contentRootPath)
    {
        var path = string.IsNullOrWhiteSpace(configuredPath)
            ? "challenges"
            : configuredPath;

        return System.IO.Path.GetFullPath(
            System.IO.Path.IsPathRooted(path)
                ? path
                : System.IO.Path.Combine(contentRootPath, path));
    }
}
