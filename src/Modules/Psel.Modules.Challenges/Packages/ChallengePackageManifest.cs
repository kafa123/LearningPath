namespace Psel.Modules.Challenges.Packages;

internal sealed class ChallengePackageManifest
{
    public int SchemaVersion { get; set; }

    public string? Slug { get; set; }

    public string? Title { get; set; }

    public string? Summary { get; set; }

    public string? DomainSlug { get; set; }

    public string? Topic { get; set; }

    public string? Difficulty { get; set; }

    public string? Type { get; set; }

    public int EstimatedMinutes { get; set; }

    public List<string>? Objectives { get; set; }

    public List<string>? Prerequisites { get; set; }

    public List<string>? CompetencySlugs { get; set; }

    public List<string>? Constraints { get; set; }

    public List<ChallengeExampleManifest>? Examples { get; set; }

    public List<ChallengeLanguageManifest>? Languages { get; set; }

    public string? Statement { get; set; }

    public string? Hints { get; set; }

    public ChallengeTestsManifest? Tests { get; set; }

    public ChallengeEvaluationManifest? Evaluation { get; set; }

    public ChallengeRubricManifest? Rubric { get; set; }

    public ChallengeExaminationManifest? Examination { get; set; }

    public ChallengeSqlManifest? Sql { get; set; }
}

internal sealed class ChallengeSqlManifest
{
    public string? Schema { get; set; }

    public string? Seed { get; set; }
}

internal sealed class ChallengeLanguageManifest
{
    public string? Id { get; set; }

    public string? Name { get; set; }

    public string? EditorLanguage { get; set; }

    public string? Harness { get; set; }

    public List<ChallengeStarterFileManifest>? StarterFiles { get; set; }
}

internal sealed class ChallengeStarterFileManifest
{
    public string? Path { get; set; }

    public string? Source { get; set; }
}

internal sealed class ChallengeExampleManifest
{
    public string? Input { get; set; }

    public string? Output { get; set; }

    public string? Explanation { get; set; }
}

internal sealed class ChallengeTestsManifest
{
    public List<ChallengeTestManifest>? Public { get; set; }

    public List<ChallengeTestManifest>? Hidden { get; set; }
}

internal sealed class ChallengeTestManifest
{
    public string? Name { get; set; }

    public string? Description { get; set; }

    public string? Source { get; set; }
}

internal sealed class ChallengeEvaluationManifest
{
    public string? Kind { get; set; }

    public int? TimeLimitSeconds { get; set; }

    public int? MemoryLimitMb { get; set; }
}

internal sealed class ChallengeRubricManifest
{
    public List<ChallengeRubricCriterionManifest>? Criteria { get; set; }
}

internal sealed class ChallengeRubricCriterionManifest
{
    public string? Id { get; set; }

    public string? Title { get; set; }

    public string? Description { get; set; }

    public int? MaxPoints { get; set; }

    public int? Weight { get; set; }
}

internal sealed class ChallengeExaminationManifest
{
    public List<ChallengeExaminationVariantManifest>? Variants { get; set; }
}

internal sealed class ChallengeExaminationVariantManifest
{
    public string? Id { get; set; }

    public string? Label { get; set; }

    public int? TimeLimitSeconds { get; set; }
}
