namespace Psel.Modules.Assessment.Domain;

/// <summary>
/// One deterministic evaluation of a submission. Test rows preserve the
/// evidence used for the score; hidden test names and data stay server-side.
/// </summary>
public sealed class Assessment
{
    private Assessment()
    {
    }

    public Guid Id { get; private set; } = Guid.NewGuid();

    public Guid SubmissionId { get; private set; }

    public string ChallengeSlug { get; private set; } = string.Empty;

    public string LanguageId { get; private set; } = string.Empty;

    /// <summary>
    /// "Run" for a practice run against public tests, "Submit" for an official
    /// attempt against public and hidden tests.
    /// </summary>
    public string Purpose { get; private set; } = string.Empty;

    public AssessmentOutcome Outcome { get; private set; }

    public int Score { get; private set; }

    public int PassedTests { get; private set; }

    public int TotalTests { get; private set; }

    public int PublicPassed { get; private set; }

    public int PublicTotal { get; private set; }

    public int HiddenPassed { get; private set; }

    public int HiddenTotal { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public ICollection<AssessmentTestResult> TestResults { get; private set; } =
        new List<AssessmentTestResult>();

    public bool IsAccepted => Outcome == AssessmentOutcome.Accepted;

    public static Assessment Create(
        Guid submissionId,
        string challengeSlug,
        string languageId,
        string purpose,
        AssessmentOutcome outcome,
        int score,
        int passedTests,
        int totalTests,
        int publicPassed,
        int publicTotal,
        int hiddenPassed,
        int hiddenTotal,
        DateTimeOffset now)
    {
        return new Assessment
        {
            SubmissionId = submissionId,
            ChallengeSlug = challengeSlug,
            LanguageId = languageId,
            Purpose = purpose,
            Outcome = outcome,
            Score = score,
            PassedTests = passedTests,
            TotalTests = totalTests,
            PublicPassed = publicPassed,
            PublicTotal = publicTotal,
            HiddenPassed = hiddenPassed,
            HiddenTotal = hiddenTotal,
            CreatedAtUtc = now
        };
    }

    public void AddTestResult(AssessmentTestResult result)
    {
        TestResults.Add(result);
    }
}
