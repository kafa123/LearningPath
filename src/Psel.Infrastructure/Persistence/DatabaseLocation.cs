namespace Psel.Infrastructure.Persistence;

/// <summary>
/// The resolved, absolute location of the SQLite database file.
/// </summary>
public sealed record DatabaseLocation(string Path);
