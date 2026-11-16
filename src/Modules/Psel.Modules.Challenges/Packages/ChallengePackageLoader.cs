using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Psel.Modules.Challenges.Contracts;

namespace Psel.Modules.Challenges.Packages;

/// <summary>
/// Loads and strictly validates trusted challenge packages from disk. The
/// loader reads only statement, hints, starter files, and public-test metadata.
/// Hidden test content and reference solutions are never read into the API
/// response model.
/// </summary>
internal sealed class ChallengePackageLoader(ChallengeLocation location)
{
    private const int MaxManifestBytes = 256 * 1024;
    private const int MaxStatementBytes = 256 * 1024;
    private const int MaxStarterFileBytes = 128 * 1024;
    private const int MaxHarnessBytes = 512 * 1024;
    private const int MaxTestBytes = 64 * 1024;
    private const int MaxSqlBytes = 128 * 1024;
    private const int MaxSqlRows = 1000;
    private const int MaxSqlColumns = 64;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        ReadCommentHandling = JsonCommentHandling.Disallow,
        AllowTrailingCommas = false
    };

    private static readonly Regex SlugPattern = new(
        "^[a-z0-9]+(?:-[a-z0-9]+)*$",
        RegexOptions.CultureInvariant);

    private static readonly HashSet<string> Difficulties =
    [
        "Beginner",
        "Intermediate",
        "Advanced"
    ];

    private static readonly HashSet<string> ChallengeTypes =
    [
        "Coding",
        "SQL",
        "Debugging",
        "Refactoring",
        "API",
        "Security",
        "Architecture",
        "Performance"
    ];

    private static readonly HashSet<string> EvaluationKinds =
    [
        "AutomatedTests",
        "Rubric",
        "Hybrid"
    ];

    public IReadOnlyList<ChallengeDetail> Load() => LoadPackages().Details;

    public ChallengePackageSet LoadPackages()
    {
        if (!Directory.Exists(location.Path))
        {
            throw new ChallengePackageValidationException(
                [$"Challenge package directory '{location.Path}' was not found."]);
        }

        List<string> manifestPaths;

        try
        {
            manifestPaths = Directory
                .EnumerateFiles(location.Path, "challenge.json", SearchOption.AllDirectories)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new ChallengePackageValidationException(
                [$"Challenge package directory '{location.Path}' could not be read."]);
        }

        if (manifestPaths.Count == 0)
        {
            throw new ChallengePackageValidationException(
                [$"No challenge packages were found under '{location.Path}'."]);
        }

        var errors = new List<string>();
        var details = new List<ChallengeDetail>();
        var harnesses = new Dictionary<string, IReadOnlyDictionary<string, string>>(
            StringComparer.OrdinalIgnoreCase);
        var testPlans = new Dictionary<string, ChallengeTestPlan>(
            StringComparer.OrdinalIgnoreCase);
        var slugs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var manifestPath in manifestPaths)
        {
            var packageRoot = Path.GetDirectoryName(manifestPath)!;
            var relativeRoot = Path.GetRelativePath(location.Path, packageRoot);
            var packageLabel = string.IsNullOrWhiteSpace(relativeRoot)
                ? "."
                : relativeRoot.Replace('\\', '/');
            var packageErrors = new List<string>();

            ChallengePackageManifest? manifest = null;

            try
            {
                var manifestJson = ReadFile(manifestPath, MaxManifestBytes);
                manifest = JsonSerializer.Deserialize<ChallengePackageManifest>(
                    manifestJson,
                    JsonOptions);
            }
            catch (JsonException exception)
            {
                packageErrors.Add($"challenge.json is not valid: {exception.Message}");
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                packageErrors.Add("challenge.json could not be read.");
            }

            if (manifest is null)
            {
                errors.AddRange(packageErrors.Select(error => $"{packageLabel}: {error}"));
                continue;
            }

            var packageHarnesses = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var packageTestPlan = new List<ChallengeExecutionTest>();
            var detail = BuildDetail(
                manifest,
                packageRoot,
                packageHarnesses,
                packageTestPlan,
                packageErrors);

            if (detail is not null)
            {
                if (slugs.TryGetValue(detail.Slug, out var existingPackage))
                {
                    packageErrors.Add(
                        $"Slug '{detail.Slug}' is already used by package '{existingPackage}'.");
                }
                else if (!string.IsNullOrWhiteSpace(detail.Slug))
                {
                    slugs.Add(detail.Slug, packageLabel);
                }
            }

            if (packageErrors.Count > 0)
            {
                errors.AddRange(packageErrors.Select(error => $"{packageLabel}: {error}"));
                continue;
            }

            if (detail is not null)
            {
                details.Add(detail);
                harnesses[detail.Slug] = packageHarnesses;
                testPlans[detail.Slug] = new ChallengeTestPlan(packageTestPlan);
            }
        }

        if (errors.Count > 0)
        {
            throw new ChallengePackageValidationException(errors);
        }

        return new ChallengePackageSet(
            details
                .OrderBy(detail => detail.Title, StringComparer.OrdinalIgnoreCase)
                .ToList(),
            harnesses,
            testPlans);
    }

    private static ChallengeDetail? BuildDetail(
        ChallengePackageManifest manifest,
        string packageRoot,
        Dictionary<string, string> harnesses,
        List<ChallengeExecutionTest> testPlan,
        List<string> errors)
    {
        if (manifest.SchemaVersion != 1)
        {
            errors.Add(
                $"schemaVersion must be 1; found {manifest.SchemaVersion}.");
        }

        var slug = RequireSlug(manifest.Slug, "slug", 100, errors);
        var title = RequireText(manifest.Title, "title", 150, errors);
        var summary = RequireText(manifest.Summary, "summary", 400, errors);
        var domainSlug = RequireSlug(manifest.DomainSlug, "domainSlug", 100, errors);
        var topic = RequireText(manifest.Topic, "topic", 100, errors);
        var difficulty = RequireText(manifest.Difficulty, "difficulty", 40, errors);
        var type = RequireText(manifest.Type, "type", 40, errors);

        if (difficulty is not null && !Difficulties.Contains(difficulty))
        {
            errors.Add(
                $"difficulty '{difficulty}' is not supported. Use Beginner, Intermediate, or Advanced.");
        }

        if (type is not null && !ChallengeTypes.Contains(type))
        {
            errors.Add(
                $"type '{type}' is not supported. Use one of: {string.Join(", ", ChallengeTypes)}.");
        }

        if (manifest.EstimatedMinutes is < 5 or > 600)
        {
            errors.Add("estimatedMinutes must be between 5 and 600.");
        }

        var objectives = ReadRequiredStringList(
            manifest.Objectives,
            "objectives",
            maxItems: 20,
            maxLength: 300,
            errors);

        var prerequisites = ReadSlugList(
            manifest.Prerequisites,
            "prerequisites",
            maxItems: 20,
            errors);

        var competencySlugs = ReadSlugList(
            manifest.CompetencySlugs,
            "competencySlugs",
            maxItems: 20,
            errors);

        var examination = ReadExamination(manifest.Examination, errors);

        var constraints = ReadStringList(
            manifest.Constraints,
            "constraints",
            maxItems: 20,
            maxLength: 300,
            errors);

        var examples = ReadExamples(manifest.Examples, errors);

        var statement = ReadPackageText(
            packageRoot,
            manifest.Statement,
            "statement",
            MaxStatementBytes,
            errors);

        string? hints = null;

        if (!string.IsNullOrWhiteSpace(manifest.Hints))
        {
            hints = ReadPackageText(
                packageRoot,
                manifest.Hints,
                "hints",
                MaxStatementBytes,
                errors);
        }

        var evaluation = ReadEvaluation(manifest.Evaluation, errors);
        var rubric = ReadRubric(manifest.Rubric, evaluation.Kind, errors);
        var sql = ReadSql(manifest.Sql, packageRoot, errors);
        var languages = ReadLanguages(
            manifest.Languages,
            packageRoot,
            sql,
            harnesses,
            errors,
            out var hasSqlLanguage);

        if (sql is not null && !hasSqlLanguage)
        {
            errors.Add("sql requires a languages entry with id 'sql'.");
        }

        var publicTests = ReadTests(
            manifest.Tests,
            evaluation.Kind,
            packageRoot,
            sql,
            testPlan,
            errors);

        return new ChallengeDetail(
            slug ?? string.Empty,
            title ?? string.Empty,
            summary ?? string.Empty,
            domainSlug ?? string.Empty,
            topic ?? string.Empty,
            difficulty ?? string.Empty,
            type ?? string.Empty,
            manifest.EstimatedMinutes,
            objectives,
            prerequisites,
            competencySlugs,
            constraints,
            examples,
            languages,
            publicTests,
            evaluation,
            rubric,
            examination,
            statement ?? string.Empty,
            hints);
    }

    private static ChallengeExaminationSummary ReadExamination(
        ChallengeExaminationManifest? manifest,
        List<string> errors)
    {
        if (manifest?.Variants is null || manifest.Variants.Count == 0)
        {
            return new ChallengeExaminationSummary(
                [new ExaminationVariantSummary("standard", "Standard", null)]);
        }

        if (manifest.Variants.Count > 5)
        {
            errors.Add("examination.variants must contain at most 5 entries.");
        }

        var variants = new List<ExaminationVariantSummary>();
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < manifest.Variants.Count; index++)
        {
            var variant = manifest.Variants[index];
            var field = $"examination.variants[{index}]";
            var id = RequireSlug(variant.Id, $"{field}.id", 60, errors);
            var label = RequireText(variant.Label, $"{field}.label", 120, errors);

            if (id is not null && !ids.Add(id))
            {
                errors.Add($"{field}.id '{id}' is duplicated.");
            }

            if (variant.TimeLimitSeconds is < 60 or > 7200)
            {
                errors.Add($"{field}.timeLimitSeconds must be between 60 and 7200 when provided.");
            }

            if (id is not null && label is not null)
            {
                variants.Add(new ExaminationVariantSummary(
                    id,
                    label,
                    variant.TimeLimitSeconds));
            }
        }

        return new ChallengeExaminationSummary(variants);
    }

    private static ChallengeRubric? ReadRubric(
        ChallengeRubricManifest? manifest,
        string evaluationKind,
        List<string> errors)
    {
        if (manifest is null)
        {
            if (string.Equals(evaluationKind, "Rubric", StringComparison.Ordinal))
            {
                errors.Add("rubric is required when evaluation.kind is Rubric.");
            }

            return null;
        }

        if (string.Equals(evaluationKind, "AutomatedTests", StringComparison.Ordinal))
        {
            errors.Add("rubric requires evaluation.kind Rubric or Hybrid.");
        }

        if (manifest.Criteria is null || manifest.Criteria.Count == 0)
        {
            errors.Add("rubric.criteria must contain at least one entry.");
            return null;
        }

        if (manifest.Criteria.Count > 12)
        {
            errors.Add("rubric.criteria must contain at most 12 entries.");
        }

        var criteria = new List<RubricCriterion>();
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < manifest.Criteria.Count; index++)
        {
            var criterion = manifest.Criteria[index];
            var field = $"rubric.criteria[{index}]";
            var id = RequireSlug(criterion.Id, $"{field}.id", 60, errors);
            var title = RequireText(criterion.Title, $"{field}.title", 120, errors);

            if (id is not null && !ids.Add(id))
            {
                errors.Add($"{field}.id '{id}' is duplicated.");
            }

            var description = string.IsNullOrWhiteSpace(criterion.Description)
                ? null
                : RequireText(criterion.Description, $"{field}.description", 500, errors);

            if (criterion.MaxPoints is null or < 1 or > 100)
            {
                errors.Add($"{field}.maxPoints must be between 1 and 100.");
            }

            if (criterion.Weight is < 1 or > 10)
            {
                errors.Add($"{field}.weight must be between 1 and 10 when provided.");
            }

            if (id is not null && title is not null && criterion.MaxPoints is >= 1 and <= 100)
            {
                criteria.Add(new RubricCriterion(
                    id,
                    title,
                    description,
                    criterion.MaxPoints.Value,
                    criterion.Weight ?? 1));
            }
        }

        return criteria.Count == 0 ? null : new ChallengeRubric(criteria);
    }

    private static ChallengeEvaluationSummary ReadEvaluation(
        ChallengeEvaluationManifest? manifest,
        List<string> errors)
    {
        if (manifest is null)
        {
            errors.Add("evaluation is required.");
            return new ChallengeEvaluationSummary(string.Empty, null, null);
        }

        var kind = RequireText(manifest.Kind, "evaluation.kind", 40, errors);

        if (kind is not null && !EvaluationKinds.Contains(kind))
        {
            errors.Add(
                $"evaluation.kind '{kind}' is not supported. Use AutomatedTests, Rubric, or Hybrid.");
        }

        if (manifest.TimeLimitSeconds is < 1 or > 120)
        {
            errors.Add("evaluation.timeLimitSeconds must be between 1 and 120.");
        }

        if (manifest.MemoryLimitMb is < 32 or > 4096)
        {
            errors.Add("evaluation.memoryLimitMb must be between 32 and 4096.");
        }

        return new ChallengeEvaluationSummary(
            kind ?? string.Empty,
            manifest.TimeLimitSeconds,
            manifest.MemoryLimitMb);
    }

    private static ChallengeSqlContent? ReadSql(
        ChallengeSqlManifest? manifest,
        string packageRoot,
        List<string> errors)
    {
        if (manifest is null)
        {
            return null;
        }

        var schema = ReadPackageText(
            packageRoot,
            manifest.Schema,
            "sql.schema",
            MaxSqlBytes,
            errors);

        if (schema is not null && string.IsNullOrWhiteSpace(schema))
        {
            errors.Add("sql.schema must not be empty.");
            schema = null;
        }

        string? seed = null;

        if (!string.IsNullOrWhiteSpace(manifest.Seed))
        {
            seed = ReadPackageText(
                packageRoot,
                manifest.Seed,
                "sql.seed",
                MaxSqlBytes,
                errors);
        }

        return schema is null ? null : new ChallengeSqlContent(schema, seed);
    }

    private static IReadOnlyList<ChallengeLanguage> ReadLanguages(
        List<ChallengeLanguageManifest>? manifests,
        string packageRoot,
        ChallengeSqlContent? sql,
        Dictionary<string, string> harnesses,
        List<string> errors,
        out bool hasSqlLanguage)
    {
        var languages = new List<ChallengeLanguage>();
        hasSqlLanguage = false;

        if (manifests is null || manifests.Count == 0)
        {
            errors.Add("languages must contain at least one entry.");
            return languages;
        }

        var languageIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < manifests.Count; index++)
        {
            var manifest = manifests[index];
            var field = $"languages[{index}]";
            var id = RequireSlug(manifest.Id, $"{field}.id", 60, errors);

            if (id is not null && !languageIds.Add(id))
            {
                errors.Add($"{field}.id '{id}' is duplicated.");
            }

            var name = RequireText(manifest.Name, $"{field}.name", 80, errors);
            var editorLanguage = string.IsNullOrWhiteSpace(manifest.EditorLanguage)
                ? id
                : RequireSlug(manifest.EditorLanguage, $"{field}.editorLanguage", 60, errors);

            string? harness = null;

            if (string.Equals(id, "sql", StringComparison.OrdinalIgnoreCase))
            {
                hasSqlLanguage = true;

                if (sql is null)
                {
                    errors.Add($"{field} uses the 'sql' language but the package has no sql block.");
                }
                else if (!string.IsNullOrWhiteSpace(manifest.Harness))
                {
                    errors.Add(
                        $"{field}.harness is not allowed for 'sql'; the server provides the harness.");
                }
                else
                {
                    harness = BuiltInSqlHarness.Source;
                }
            }
            else if (!string.IsNullOrWhiteSpace(manifest.Harness))
            {
                harness = ReadPackageText(
                    packageRoot,
                    manifest.Harness,
                    $"{field}.harness",
                    MaxHarnessBytes,
                    errors);

                if (harness is not null && string.IsNullOrWhiteSpace(harness))
                {
                    errors.Add($"{field}.harness must not be empty.");
                    harness = null;
                }
            }

            var files = new List<ChallengeFile>();

            if (manifest.StarterFiles is null || manifest.StarterFiles.Count == 0)
            {
                errors.Add($"{field}.starterFiles must contain at least one entry.");
            }
            else
            {
                var filePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                for (var fileIndex = 0; fileIndex < manifest.StarterFiles.Count; fileIndex++)
                {
                    var fileManifest = manifest.StarterFiles[fileIndex];
                    var fileField = $"{field}.starterFiles[{fileIndex}]";
                    var filePath = RequireRelativePath(
                        fileManifest.Path,
                        $"{fileField}.path",
                        errors);

                    if (filePath is not null && !filePaths.Add(filePath))
                    {
                        errors.Add($"{fileField}.path '{filePath}' is duplicated.");
                    }

                    var content = ReadPackageText(
                        packageRoot,
                        fileManifest.Source,
                        $"{fileField}.source",
                        MaxStarterFileBytes,
                        errors);

                    if (filePath is not null && content is not null)
                    {
                        files.Add(new ChallengeFile(filePath, content));
                    }
                }
            }

            var languageId = id ?? string.Empty;

            if (harness is not null && !string.IsNullOrEmpty(languageId))
            {
                harnesses[languageId] = harness;
            }

            languages.Add(new ChallengeLanguage(
                languageId,
                name ?? string.Empty,
                editorLanguage ?? string.Empty,
                files,
                harness is not null));
        }

        return languages;
    }

    private static IReadOnlyList<PublicTestSummary> ReadTests(
        ChallengeTestsManifest? manifest,
        string evaluationKind,
        string packageRoot,
        ChallengeSqlContent? sql,
        List<ChallengeExecutionTest> testPlan,
        List<string> errors)
    {
        if (manifest is null)
        {
            if (!string.Equals(evaluationKind, "Rubric", StringComparison.Ordinal))
            {
                errors.Add("tests is required.");
            }

            return [];
        }

        var publicTests = ReadTestReferences(
            manifest.Public,
            "tests.public",
            "public",
            "public",
            packageRoot,
            sql,
            testPlan,
            errors,
            out var publicCount);

        ReadTestReferences(
            manifest.Hidden,
            "tests.hidden",
            "hidden",
            "hidden",
            packageRoot,
            sql,
            testPlan,
            errors,
            out var hiddenCount);

        if (evaluationKind is "AutomatedTests" or "Hybrid")
        {
            if (publicCount == 0)
            {
                errors.Add("tests.public must contain at least one test for automated evaluation.");
            }

            if (hiddenCount == 0)
            {
                errors.Add("tests.hidden must contain at least one test for automated evaluation.");
            }
        }

        return publicTests;
    }

    private static IReadOnlyList<PublicTestSummary> ReadTestReferences(
        List<ChallengeTestManifest>? manifests,
        string field,
        string visibility,
        string idPrefix,
        string packageRoot,
        ChallengeSqlContent? sql,
        List<ChallengeExecutionTest> testPlan,
        List<string> errors,
        out int count)
    {
        var tests = new List<PublicTestSummary>();
        count = 0;

        if (manifests is null)
        {
            return tests;
        }

        count = manifests.Count;
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < manifests.Count; index++)
        {
            var manifest = manifests[index];
            var testField = $"{field}[{index}]";
            var name = RequireText(manifest.Name, $"{testField}.name", 120, errors);

            if (name is not null && !names.Add(name))
            {
                errors.Add($"{testField}.name '{name}' is duplicated.");
            }

            var description = string.IsNullOrWhiteSpace(manifest.Description)
                ? null
                : RequireText(manifest.Description, $"{testField}.description", 400, errors);

            var data = ReadTestData(
                packageRoot,
                manifest.Source,
                $"{testField}.source",
                sql,
                errors);

            if (name is not null && data is not null)
            {
                testPlan.Add(new ChallengeExecutionTest(
                    $"{idPrefix}-{index + 1}",
                    visibility,
                    name,
                    description,
                    data));

                if (visibility == "public")
                {
                    tests.Add(new PublicTestSummary(name, description));
                }
            }
        }

        return tests;
    }

    private static string? ReadTestData(
        string packageRoot,
        string? relativePath,
        string field,
        ChallengeSqlContent? sql,
        List<string> errors)
    {
        var content = ReadPackageText(packageRoot, relativePath, field, MaxTestBytes, errors);

        if (content is null)
        {
            return null;
        }

        if (sql is not null)
        {
            return ReadSqlTestData(content, field, sql, errors);
        }

        try
        {
            using var document = JsonDocument.Parse(content);

            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                errors.Add($"{field} must contain a JSON object.");
                return null;
            }

            var hasExpectation = document.RootElement.TryGetProperty("expected", out _)
                || document.RootElement.TryGetProperty("cases", out _)
                || document.RootElement.TryGetProperty("expectedStatus", out _);

            if (!hasExpectation)
            {
                errors.Add(
                    $"{field} must define expected, cases, or expectedStatus for assessment.");
                return null;
            }
        }
        catch (JsonException)
        {
            errors.Add($"{field} is not valid JSON.");
            return null;
        }

        return content;
    }

    private static string? ReadSqlTestData(
        string content,
        string field,
        ChallengeSqlContent sql,
        List<string> errors)
    {
        JsonObject root;

        try
        {
            root = JsonNode.Parse(content) as JsonObject
                ?? throw new JsonException();
        }
        catch (JsonException)
        {
            errors.Add($"{field} must contain a JSON object.");
            return null;
        }

        var errorCount = errors.Count;

        if (root.ContainsKey("expected"))
        {
            ValidateSqlExpected(root["expected"], $"{field}.expected", errors);
        }

        if (root.ContainsKey("plan"))
        {
            ValidateSqlPlan(root["plan"], $"{field}.plan", errors);
        }

        if (!root.ContainsKey("expected") && !root.ContainsKey("plan"))
        {
            errors.Add($"{field} must define expected or plan for SQL assessment.");
        }

        if (root["columns"] is { } columns)
        {
            ValidateSqlColumns(columns, $"{field}.columns", errors);
        }

        if (root["ordered"] is { } ordered
            && (ordered is not JsonValue orderedValue
                || !orderedValue.TryGetValue<bool>(out _)))
        {
            errors.Add($"{field}.ordered must be a boolean.");
        }

        if (root.ContainsKey("schema"))
        {
            ValidateSqlText(root["schema"], $"{field}.schema", errors);
        }
        else
        {
            root["schema"] = sql.Schema;
        }

        if (root.ContainsKey("seed"))
        {
            ValidateSqlText(root["seed"], $"{field}.seed", errors);
        }
        else if (sql.Seed is not null)
        {
            root["seed"] = sql.Seed;
        }

        if (errors.Count > errorCount)
        {
            return null;
        }

        return root.ToJsonString();
    }

    private static void ValidateSqlExpected(
        JsonNode? node,
        string field,
        List<string> errors)
    {
        if (node is not JsonArray rows)
        {
            errors.Add($"{field} must be an array of rows.");
            return;
        }

        if (rows.Count > MaxSqlRows)
        {
            errors.Add($"{field} must contain at most {MaxSqlRows} rows.");
        }

        for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            if (rows[rowIndex] is not JsonArray values)
            {
                errors.Add($"{field}[{rowIndex}] must be an array of scalar values.");
                continue;
            }

            if (values.Count > MaxSqlColumns)
            {
                errors.Add(
                    $"{field}[{rowIndex}] must contain at most {MaxSqlColumns} columns.");
            }

            for (var valueIndex = 0; valueIndex < values.Count; valueIndex++)
            {
                var value = values[valueIndex];

                if (value is null)
                {
                    continue;
                }

                if (value.GetValueKind() is not (
                    JsonValueKind.String
                    or JsonValueKind.Number
                    or JsonValueKind.True
                    or JsonValueKind.False))
                {
                    errors.Add(
                        $"{field}[{rowIndex}][{valueIndex}] must be a string, number, boolean, or null.");
                }
            }
        }
    }

    private static void ValidateSqlPlan(
        JsonNode? node,
        string field,
        List<string> errors)
    {
        if (node is not JsonObject plan)
        {
            errors.Add($"{field} must be an object.");
            return;
        }

        var mustContain = ReadSqlPlanList(plan["mustContain"], $"{field}.mustContain", errors);
        var mustNotContain = ReadSqlPlanList(
            plan["mustNotContain"],
            $"{field}.mustNotContain",
            errors);

        if (mustContain == 0 && mustNotContain == 0)
        {
            errors.Add($"{field} must define mustContain or mustNotContain.");
        }
    }

    private static int ReadSqlPlanList(
        JsonNode? node,
        string field,
        List<string> errors)
    {
        if (node is null)
        {
            return 0;
        }

        if (node is not JsonArray values)
        {
            errors.Add($"{field} must be an array of strings.");
            return 0;
        }

        if (values.Count > 20)
        {
            errors.Add($"{field} must contain at most 20 entries.");
        }

        for (var index = 0; index < values.Count; index++)
        {
            if (values[index] is not JsonValue value
                || !value.TryGetValue<string>(out var text)
                || string.IsNullOrWhiteSpace(text)
                || text.Length > 200)
            {
                errors.Add($"{field}[{index}] must be a non-empty string of at most 200 characters.");
            }
        }

        return values.Count;
    }

    private static void ValidateSqlColumns(
        JsonNode node,
        string field,
        List<string> errors)
    {
        if (node is not JsonArray values)
        {
            errors.Add($"{field} must be an array of column names.");
            return;
        }

        if (values.Count > MaxSqlColumns)
        {
            errors.Add($"{field} must contain at most {MaxSqlColumns} entries.");
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < values.Count; index++)
        {
            if (values[index] is not JsonValue value
                || !value.TryGetValue<string>(out var text)
                || string.IsNullOrWhiteSpace(text)
                || text.Length > 120)
            {
                errors.Add($"{field}[{index}] must be a non-empty string of at most 120 characters.");
                continue;
            }

            if (!seen.Add(text))
            {
                errors.Add($"{field}[{index}] '{text}' is duplicated.");
            }
        }
    }

    private static void ValidateSqlText(
        JsonNode? node,
        string field,
        List<string> errors)
    {
        if (node is not JsonValue value
            || !value.TryGetValue<string>(out var text)
            || string.IsNullOrWhiteSpace(text))
        {
            errors.Add($"{field} must be a non-empty SQL string.");
            return;
        }

        if (text.Length > MaxSqlBytes)
        {
            errors.Add($"{field} exceeds the maximum size of {MaxSqlBytes} bytes.");
        }
    }

    private static IReadOnlyList<ChallengeExample> ReadExamples(
        List<ChallengeExampleManifest>? manifests,
        List<string> errors)
    {
        var examples = new List<ChallengeExample>();

        if (manifests is null)
        {
            return examples;
        }

        if (manifests.Count > 10)
        {
            errors.Add("examples must contain at most 10 entries.");
        }

        for (var index = 0; index < manifests.Count; index++)
        {
            var manifest = manifests[index];
            var field = $"examples[{index}]";
            var input = RequireText(manifest.Input, $"{field}.input", 500, errors);
            var output = RequireText(manifest.Output, $"{field}.output", 500, errors);
            var explanation = string.IsNullOrWhiteSpace(manifest.Explanation)
                ? null
                : RequireText(manifest.Explanation, $"{field}.explanation", 1000, errors);

            if (input is not null && output is not null)
            {
                examples.Add(new ChallengeExample(input, output, explanation));
            }
        }

        return examples;
    }

    private static List<string> ReadRequiredStringList(
        List<string>? values,
        string field,
        int maxItems,
        int maxLength,
        List<string> errors)
    {
        var result = ReadStringList(values, field, maxItems, maxLength, errors);

        if (result.Count == 0)
        {
            errors.Add($"{field} must contain at least one entry.");
        }

        return result;
    }

    private static List<string> ReadStringList(
        List<string>? values,
        string field,
        int maxItems,
        int maxLength,
        List<string> errors)
    {
        var result = new List<string>();

        if (values is null)
        {
            return result;
        }

        if (values.Count > maxItems)
        {
            errors.Add($"{field} must contain at most {maxItems} entries.");
        }

        for (var index = 0; index < values.Count; index++)
        {
            var value = RequireText(values[index], $"{field}[{index}]", maxLength, errors);

            if (value is not null)
            {
                result.Add(value);
            }
        }

        return result;
    }

    private static List<string> ReadSlugList(
        List<string>? values,
        string field,
        int maxItems,
        List<string> errors)
    {
        var result = new List<string>();

        if (values is null)
        {
            return result;
        }

        if (values.Count > maxItems)
        {
            errors.Add($"{field} must contain at most {maxItems} entries.");
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < values.Count; index++)
        {
            var value = RequireSlug(values[index], $"{field}[{index}]", 100, errors);

            if (value is not null && !seen.Add(value))
            {
                errors.Add($"{field}[{index}] '{value}' is duplicated.");
            }

            if (value is not null)
            {
                result.Add(value);
            }
        }

        return result;
    }

    private static string? ReadPackageText(
        string packageRoot,
        string? relativePath,
        string field,
        int maxBytes,
        List<string> errors)
    {
        var fullPath = ResolveInsidePackage(packageRoot, relativePath, field, errors);

        if (fullPath is null)
        {
            return null;
        }

        var fileInfo = new FileInfo(fullPath);

        if (fileInfo.Length > maxBytes)
        {
            errors.Add($"{field} exceeds the maximum size of {maxBytes} bytes.");
            return null;
        }

        try
        {
            return File.ReadAllText(fullPath);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            errors.Add($"{field} could not be read.");
            return null;
        }
    }

    private static string ReadFile(string path, int maxBytes)
    {
        var fileInfo = new FileInfo(path);

        if (fileInfo.Length > maxBytes)
        {
            throw new InvalidOperationException(
                $"File '{Path.GetFileName(path)}' exceeds {maxBytes} bytes.");
        }

        return File.ReadAllText(path);
    }

    private static string? ResolveInsidePackage(
        string packageRoot,
        string? relativePath,
        string field,
        List<string> errors)
    {
        var normalizedPath = RequireRelativePath(relativePath, field, errors);

        if (normalizedPath is null)
        {
            return null;
        }

        var root = Path.GetFullPath(packageRoot);
        var fullPath = Path.GetFullPath(Path.Combine(root, normalizedPath));
        var rootPrefix = root.EndsWith(Path.DirectorySeparatorChar)
            ? root
            : root + Path.DirectorySeparatorChar;
        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        if (!fullPath.StartsWith(rootPrefix, comparison))
        {
            errors.Add($"{field} must stay inside the package directory.");
            return null;
        }

        if (!File.Exists(fullPath))
        {
            errors.Add($"{field} points to a missing file: {normalizedPath}.");
            return null;
        }

        return fullPath;
    }

    private static string? RequireRelativePath(
        string? value,
        string field,
        List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add($"{field} is required.");
            return null;
        }

        var normalized = value.Trim().Replace('\\', '/');

        if (Path.IsPathRooted(normalized)
            || normalized.Contains(':', StringComparison.Ordinal)
            || normalized.Split('/').Any(part => part is "" or "." or ".."))
        {
            errors.Add($"{field} must be a relative path inside the package.");
            return null;
        }

        return normalized;
    }

    private static string? RequireSlug(
        string? value,
        string field,
        int maxLength,
        List<string> errors)
    {
        var text = RequireText(value, field, maxLength, errors);

        if (text is not null && !SlugPattern.IsMatch(text))
        {
            errors.Add($"{field} must use lowercase letters, numbers, and single hyphens.");
            return null;
        }

        return text;
    }

    private static string? RequireText(
        string? value,
        string field,
        int maxLength,
        List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add($"{field} is required.");
            return null;
        }

        var text = value.Trim();

        if (text.Length > maxLength)
        {
            errors.Add($"{field} must be at most {maxLength} characters.");
            return null;
        }

        return text;
    }
}

internal sealed record ChallengeSqlContent(string Schema, string? Seed);
