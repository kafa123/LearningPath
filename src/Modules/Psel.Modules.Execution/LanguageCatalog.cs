namespace Psel.Modules.Execution;

internal sealed class LanguageCatalog : ILanguageCatalog
{
    private static readonly IReadOnlyList<SupportedLanguage> Languages =
    [
        new SupportedLanguage("csharp", "C#"),
        new SupportedLanguage("python", "Python"),
        new SupportedLanguage("sql", "SQL"),
        new SupportedLanguage("go", "Go")
    ];

    public IReadOnlyList<SupportedLanguage> GetLanguages() => Languages;
}
