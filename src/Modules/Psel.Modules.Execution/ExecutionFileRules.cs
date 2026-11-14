namespace Psel.Modules.Execution;

/// <summary>
/// Shared filename rules for submitted execution files. The submission API and
/// the runner both apply these checks so a request cannot escape its workspace
/// or overwrite server-generated files.
/// </summary>
public static class ExecutionFileRules
{
    private static readonly HashSet<string> ReservedFileNames =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "Submission.csproj",
            "PselHarness.cs",
            "PselHarness.py",
            "PselTests.json"
        };

    public static bool IsSafeRelativePath(string? path, out string normalized)
    {
        normalized = string.Empty;

        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        var candidate = path.Trim().Replace('\\', '/');

        if (candidate.Length > 200
            || candidate.StartsWith('/')
            || candidate.Contains(':', StringComparison.Ordinal))
        {
            return false;
        }

        var parts = candidate.Split('/');

        if (parts.Length == 0
            || parts.Any(part => part is "" or "." or "..")
            || parts.Any(part => part.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0))
        {
            return false;
        }

        if (ReservedFileNames.Contains(parts[^1]))
        {
            return false;
        }

        normalized = candidate;
        return true;
    }
}
