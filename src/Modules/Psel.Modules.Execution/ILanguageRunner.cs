namespace Psel.Modules.Execution;

/// <summary>
/// Language-specific execution adapter. The API host and submission module only
/// depend on this contract; Docker details stay behind the runner.
/// </summary>
public interface ILanguageRunner
{
    string LanguageId { get; }

    /// <summary>
    /// Maximum number of source files accepted for one submission. Languages
    /// that execute a single artifact, such as SQL, override this limit.
    /// </summary>
    int MaxSubmissionFiles => 10;

    bool AcceptsFile(string path);

    Task<ExecutionOutcome> ExecuteAsync(
        ExecutionRequest request,
        CancellationToken cancellationToken);
}

/// <summary>
/// Languages the server is configured to run. This is trusted server-side
/// configuration, never a client-supplied value.
/// </summary>
public interface ILanguageCatalog
{
    IReadOnlyList<SupportedLanguage> GetLanguages();
}
