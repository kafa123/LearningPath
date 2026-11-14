namespace Psel.Infrastructure.Persistence;

/// <summary>
/// Resolves the configured SQLite database path to a stable absolute path.
/// </summary>
public static class DatabasePathResolver
{
    public const string DefaultRelativePath = "data/psel.db";

    public static string Resolve(string? configuredPath, string contentRootPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contentRootPath);

        var path = string.IsNullOrWhiteSpace(configuredPath)
            ? DefaultRelativePath
            : configuredPath.Trim();

        return Path.IsPathRooted(path)
            ? Path.GetFullPath(path)
            : Path.GetFullPath(Path.Combine(contentRootPath, path));
    }
}
