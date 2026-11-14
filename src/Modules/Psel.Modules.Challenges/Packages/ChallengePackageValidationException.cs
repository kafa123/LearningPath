namespace Psel.Modules.Challenges.Packages;

/// <summary>
/// Raised when one or more challenge packages violate the schema or reference
/// missing or unsafe files. Startup fails so invalid content never becomes
/// browsable.
/// </summary>
public sealed class ChallengePackageValidationException : Exception
{
    public ChallengePackageValidationException(IReadOnlyList<string> errors)
        : base($"Challenge package validation failed:{Environment.NewLine}- "
            + string.Join($"{Environment.NewLine}- ", errors))
    {
        Errors = errors;
    }

    public IReadOnlyList<string> Errors { get; }
}
