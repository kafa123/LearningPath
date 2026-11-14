namespace Psel.Infrastructure.Persistence;

/// <summary>
/// Configuration values for the application database.
/// </summary>
public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    /// <summary>
    /// Gets or sets the configured database path. Relative paths are resolved
    /// against the API content root.
    /// </summary>
    public string Path { get; set; } = DatabasePathResolver.DefaultRelativePath;
}
