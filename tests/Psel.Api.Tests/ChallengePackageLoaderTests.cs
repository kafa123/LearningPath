using System.Text.Json;
using System.Text.Json.Nodes;
using Psel.Modules.Challenges.Packages;

namespace Psel.Api.Tests;

public sealed class ChallengePackageLoaderTests
{
    [Fact]
    public void Load_ValidPackage_ProjectsOnlyClientSafeContent()
    {
        using var root = new TemporaryDirectory();
        ChallengeTestContent.AddPackage(root.Path);

        var detail = Assert.Single(
            new ChallengePackageLoader(new ChallengeLocation(root.Path)).Load());

        Assert.Equal("sample-challenge", detail.Slug);
        Assert.Equal("Sample Challenge", detail.Title);
        Assert.Equal("programming-fundamentals", detail.DomainSlug);
        Assert.Equal("Beginner", detail.Difficulty);
        Assert.Equal("Coding", detail.Type);
        Assert.Equal(15, detail.EstimatedMinutes);
        Assert.Equal("# Sample statement", detail.StatementMarkdown);
        Assert.Equal("# Sample hints", detail.HintsMarkdown);
        Assert.Single(detail.Objectives);
        Assert.Single(detail.Constraints);
        Assert.Single(detail.Examples);

        var language = Assert.Single(detail.Languages);
        Assert.Equal("csharp", language.Id);
        Assert.Equal("csharp", language.EditorLanguage);
        var starter = Assert.Single(language.StarterFiles);
        Assert.Equal("Solution.cs", starter.Path);
        Assert.Equal("// starter marker", starter.Content);

        var publicTest = Assert.Single(detail.PublicTests);
        Assert.Equal("One", publicTest.Name);

        Assert.Equal("AutomatedTests", detail.Evaluation.Kind);
        Assert.Equal(5, detail.Evaluation.TimeLimitSeconds);

        var serialized = JsonSerializer.Serialize(detail);
        Assert.DoesNotContain(ChallengeTestContent.HiddenSentinel, serialized, StringComparison.Ordinal);
        Assert.DoesNotContain(ChallengeTestContent.SolutionSentinel, serialized, StringComparison.Ordinal);
        Assert.DoesNotContain("tests/hidden", serialized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("solutions/", serialized, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Load_MissingDirectory_ThrowsValidationException()
    {
        using var root = new TemporaryDirectory();
        var missingPath = Path.Combine(root.Path, "missing");

        var exception = Assert.Throws<ChallengePackageValidationException>(
            () => new ChallengePackageLoader(new ChallengeLocation(missingPath)).Load());

        Assert.Contains(
            "was not found",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Load_NoPackages_ThrowsValidationException()
    {
        using var root = new TemporaryDirectory();

        var exception = Assert.Throws<ChallengePackageValidationException>(
            () => new ChallengePackageLoader(new ChallengeLocation(root.Path)).Load());

        Assert.Contains(
            "No challenge packages were found",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Load_UnsupportedSchemaVersion_Throws()
    {
        using var root = new TemporaryDirectory();
        ChallengeTestContent.AddPackage(
            root.Path,
            ChallengeTestContent.MinimalManifest.Replace(
                "\"schemaVersion\": 1,",
                "\"schemaVersion\": 2,",
                StringComparison.Ordinal));

        var exception = Assert.Throws<ChallengePackageValidationException>(
            () => new ChallengePackageLoader(new ChallengeLocation(root.Path)).Load());

        Assert.Contains("schemaVersion must be 1", exception.Message);
    }

    [Fact]
    public void Load_UnknownManifestField_ThrowsStrictJsonError()
    {
        using var root = new TemporaryDirectory();
        ChallengeTestContent.AddPackage(
            root.Path,
            ChallengeTestContent.MinimalManifest.Replace(
                "\"schemaVersion\": 1,",
                "\"schemaVersion\": 1, \"unexpectedField\": true,",
                StringComparison.Ordinal));

        var exception = Assert.Throws<ChallengePackageValidationException>(
            () => new ChallengePackageLoader(new ChallengeLocation(root.Path)).Load());

        Assert.Contains("challenge.json is not valid", exception.Message);
    }

    [Fact]
    public void Load_MissingStarterFile_Throws()
    {
        using var root = new TemporaryDirectory();
        var packageRoot = ChallengeTestContent.AddPackage(root.Path);
        File.Delete(Path.Combine(packageRoot, "starter", "Solution.cs"));

        var exception = Assert.Throws<ChallengePackageValidationException>(
            () => new ChallengePackageLoader(new ChallengeLocation(root.Path)).Load());

        Assert.Contains("points to a missing file", exception.Message);
    }

    [Fact]
    public void Load_MissingHiddenTestFile_Throws()
    {
        using var root = new TemporaryDirectory();
        var packageRoot = ChallengeTestContent.AddPackage(root.Path);
        File.Delete(Path.Combine(packageRoot, "tests", "hidden", "hidden.json"));

        var exception = Assert.Throws<ChallengePackageValidationException>(
            () => new ChallengePackageLoader(new ChallengeLocation(root.Path)).Load());

        Assert.Contains("tests.hidden[0].source points to a missing file", exception.Message);
    }

    [Fact]
    public void Load_StarterPathOutsidePackage_Throws()
    {
        using var root = new TemporaryDirectory();
        ChallengeTestContent.AddPackage(
            root.Path,
            ChallengeTestContent.MinimalManifest.Replace(
                "\"source\": \"starter/Solution.cs\"",
                "\"source\": \"../outside.cs\"",
                StringComparison.Ordinal));

        var exception = Assert.Throws<ChallengePackageValidationException>(
            () => new ChallengePackageLoader(new ChallengeLocation(root.Path)).Load());

        Assert.Contains(
            "must be a relative path inside the package",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Load_HiddenTestPathOutsidePackage_Throws()
    {
        using var root = new TemporaryDirectory();
        ChallengeTestContent.AddPackage(
            root.Path,
            ChallengeTestContent.MinimalManifest.Replace(
                "\"source\": \"tests/hidden/hidden.json\"",
                "\"source\": \"../../escape.json\"",
                StringComparison.Ordinal));

        var exception = Assert.Throws<ChallengePackageValidationException>(
            () => new ChallengePackageLoader(new ChallengeLocation(root.Path)).Load());

        Assert.Contains(
            "must be a relative path inside the package",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Load_DuplicateSlug_Throws()
    {
        using var root = new TemporaryDirectory();
        ChallengeTestContent.AddPackage(root.Path, packageName: "first");
        ChallengeTestContent.AddPackage(root.Path, packageName: "second");

        var exception = Assert.Throws<ChallengePackageValidationException>(
            () => new ChallengePackageLoader(new ChallengeLocation(root.Path)).Load());

        Assert.Contains("already used by package 'first'", exception.Message);
    }

    [Fact]
    public void Load_AutomatedEvaluationWithoutHiddenTests_Throws()
    {
        using var root = new TemporaryDirectory();
        var manifest = JsonNode.Parse(ChallengeTestContent.MinimalManifest)!.AsObject();
        manifest["tests"]!["hidden"] = new JsonArray();
        ChallengeTestContent.AddPackage(root.Path, manifest.ToJsonString());

        var exception = Assert.Throws<ChallengePackageValidationException>(
            () => new ChallengePackageLoader(new ChallengeLocation(root.Path)).Load());

        Assert.Contains(
            "tests.hidden must contain at least one test",
            exception.Message);
    }

    [Fact]
    public void Load_LanguageEditorLanguageDefaultsToId()
    {
        using var root = new TemporaryDirectory();
        ChallengeTestContent.AddPackage(
            root.Path,
            ChallengeTestContent.MinimalManifest.Replace(
                "\"editorLanguage\": \"csharp\",",
                string.Empty,
                StringComparison.Ordinal));

        var detail = Assert.Single(
            new ChallengePackageLoader(new ChallengeLocation(root.Path)).Load());

        Assert.Equal("csharp", detail.Languages[0].EditorLanguage);
    }

    [Fact]
    public void Load_RubricPackage_ProjectsClientSafeCriteria()
    {
        using var root = new TemporaryDirectory();
        var manifest = JsonNode.Parse(ChallengeTestContent.MinimalManifest)!.AsObject();
        manifest["evaluation"]!["kind"] = "Hybrid";
        manifest["rubric"] = JsonNode.Parse(
            """
            {
              "criteria": [
                {
                  "id": "correctness",
                  "title": "Correctness",
                  "description": "Behavior is preserved.",
                  "maxPoints": 10,
                  "weight": 3
                },
                {
                  "id": "clarity",
                  "title": "Clarity",
                  "maxPoints": 5
                }
              ]
            }
            """);
        ChallengeTestContent.AddPackage(root.Path, manifest.ToJsonString());

        var detail = Assert.Single(
            new ChallengePackageLoader(new ChallengeLocation(root.Path)).Load());

        Assert.NotNull(detail.Rubric);
        Assert.Equal(2, detail.Rubric.Criteria.Count);
        Assert.Equal("correctness", detail.Rubric.Criteria[0].Id);
        Assert.Equal("Behavior is preserved.", detail.Rubric.Criteria[0].Description);
        Assert.Equal(10, detail.Rubric.Criteria[0].MaxPoints);
        Assert.Equal(3, detail.Rubric.Criteria[0].Weight);
        Assert.Null(detail.Rubric.Criteria[1].Description);
        Assert.Equal(1, detail.Rubric.Criteria[1].Weight);

        var serialized = JsonSerializer.Serialize(
            detail,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.Contains("\"rubric\"", serialized, StringComparison.Ordinal);
        Assert.DoesNotContain(ChallengeTestContent.HiddenSentinel, serialized, StringComparison.Ordinal);
        Assert.DoesNotContain(ChallengeTestContent.SolutionSentinel, serialized, StringComparison.Ordinal);
    }

    [Fact]
    public void Load_AutomatedEvaluationWithRubric_Throws()
    {
        using var root = new TemporaryDirectory();
        var manifest = JsonNode.Parse(ChallengeTestContent.MinimalManifest)!.AsObject();
        manifest["rubric"] = JsonNode.Parse(
            """
            {
              "criteria": [
                { "id": "clarity", "title": "Clarity", "maxPoints": 10 }
              ]
            }
            """);
        ChallengeTestContent.AddPackage(root.Path, manifest.ToJsonString());

        var exception = Assert.Throws<ChallengePackageValidationException>(
            () => new ChallengePackageLoader(new ChallengeLocation(root.Path)).Load());

        Assert.Contains(
            "rubric requires evaluation.kind Rubric or Hybrid",
            exception.Message);
    }

    [Fact]
    public void Load_RubricEvaluationWithoutRubric_Throws()
    {
        using var root = new TemporaryDirectory();
        var manifest = JsonNode.Parse(ChallengeTestContent.MinimalManifest)!.AsObject();
        manifest["evaluation"]!["kind"] = "Rubric";
        ChallengeTestContent.AddPackage(root.Path, manifest.ToJsonString());

        var exception = Assert.Throws<ChallengePackageValidationException>(
            () => new ChallengePackageLoader(new ChallengeLocation(root.Path)).Load());

        Assert.Contains(
            "rubric is required when evaluation.kind is Rubric",
            exception.Message);
    }

    [Fact]
    public void Load_RubricCriterionOutOfRange_Throws()
    {
        using var root = new TemporaryDirectory();
        var manifest = JsonNode.Parse(ChallengeTestContent.MinimalManifest)!.AsObject();
        manifest["evaluation"]!["kind"] = "Hybrid";
        manifest["rubric"] = JsonNode.Parse(
            """
            {
              "criteria": [
                { "id": "clarity", "title": "Clarity", "maxPoints": 0, "weight": 11 }
              ]
            }
            """);
        ChallengeTestContent.AddPackage(root.Path, manifest.ToJsonString());

        var exception = Assert.Throws<ChallengePackageValidationException>(
            () => new ChallengePackageLoader(new ChallengeLocation(root.Path)).Load());

        Assert.Contains(
            "rubric.criteria[0].maxPoints must be between 1 and 100",
            exception.Message);
        Assert.Contains(
            "rubric.criteria[0].weight must be between 1 and 10",
            exception.Message);
    }

    [Fact]
    public void Load_SqlPackage_UsesBuiltInHarnessAndInjectsSchema()
    {
        using var root = new TemporaryDirectory();
        ChallengeTestContent.AddSqlPackage(root.Path);

        var packages = new ChallengePackageLoader(new ChallengeLocation(root.Path))
            .LoadPackages();
        var detail = Assert.Single(packages.Details);

        var language = Assert.Single(detail.Languages);
        Assert.Equal("sql", language.Id);
        Assert.Equal("sql", language.EditorLanguage);
        Assert.True(language.CanExecute);

        Assert.True(packages.Harnesses["sample-sql"].TryGetValue("sql", out var harness));
        Assert.Contains("__PSEL_RESULTS_BEGIN__", harness, StringComparison.Ordinal);
        Assert.Contains("PSEL_SQL_COMPILE_ERROR", harness, StringComparison.Ordinal);

        var tests = packages.TestPlans["sample-sql"].Tests;
        Assert.Equal(2, tests.Count);
        Assert.Contains(ChallengeTestContent.SqlSchemaSentinel, tests[0].DataJson, StringComparison.Ordinal);
        Assert.Contains(ChallengeTestContent.SqlSeedSentinel, tests[0].DataJson, StringComparison.Ordinal);

        var serialized = JsonSerializer.Serialize(detail);
        Assert.DoesNotContain(
            ChallengeTestContent.HiddenSqlSentinel,
            serialized,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            ChallengeTestContent.SqlSchemaSentinel,
            serialized,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            ChallengeTestContent.SqlSeedSentinel,
            serialized,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Load_SqlLanguageWithoutSqlBlock_Throws()
    {
        using var root = new TemporaryDirectory();
        var manifest = JsonNode.Parse(ChallengeTestContent.MinimalSqlManifest)!.AsObject();
        manifest.Remove("sql");
        ChallengeTestContent.AddSqlPackage(root.Path, manifest.ToJsonString());

        var exception = Assert.Throws<ChallengePackageValidationException>(
            () => new ChallengePackageLoader(new ChallengeLocation(root.Path)).Load());

        Assert.Contains("has no sql block", exception.Message);
    }

    [Fact]
    public void Load_SqlBlockWithoutSqlLanguage_Throws()
    {
        using var root = new TemporaryDirectory();
        var manifest = JsonNode.Parse(ChallengeTestContent.MinimalSqlManifest)!.AsObject();
        manifest["languages"]![0]!["id"] = "csharp";
        manifest["languages"]![0]!["editorLanguage"] = "csharp";
        manifest["languages"]![0]!["starterFiles"]![0]!["path"] = "Solution.cs";
        manifest["languages"]![0]!["starterFiles"]![0]!["source"] = "starter/sql/query.sql";
        ChallengeTestContent.AddSqlPackage(root.Path, manifest.ToJsonString());

        var exception = Assert.Throws<ChallengePackageValidationException>(
            () => new ChallengePackageLoader(new ChallengeLocation(root.Path)).Load());

        Assert.Contains("sql requires a languages entry", exception.Message);
    }

    [Fact]
    public void Load_SqlLanguageWithAuthorHarness_Throws()
    {
        using var root = new TemporaryDirectory();
        var manifest = JsonNode.Parse(ChallengeTestContent.MinimalSqlManifest)!.AsObject();
        manifest["languages"]![0]!["harness"] = "harness/sql/PselHarness.py";
        ChallengeTestContent.AddSqlPackage(root.Path, manifest.ToJsonString());

        var exception = Assert.Throws<ChallengePackageValidationException>(
            () => new ChallengePackageLoader(new ChallengeLocation(root.Path)).Load());

        Assert.Contains("harness is not allowed for 'sql'", exception.Message);
    }

    [Fact]
    public void Load_SqlTestWithoutExpectationOrPlan_Throws()
    {
        using var root = new TemporaryDirectory();
        var packageRoot = ChallengeTestContent.AddSqlPackage(root.Path);
        File.WriteAllText(
            Path.Combine(packageRoot, "tests", "hidden", "hidden.json"),
            """{"columns":["value"]}""");

        var exception = Assert.Throws<ChallengePackageValidationException>(
            () => new ChallengePackageLoader(new ChallengeLocation(root.Path)).Load());

        Assert.Contains("must define expected or plan", exception.Message);
    }

    [Fact]
    public void Load_SqlTestWithMalformedPlan_Throws()
    {
        using var root = new TemporaryDirectory();
        var packageRoot = ChallengeTestContent.AddSqlPackage(root.Path);
        File.WriteAllText(
            Path.Combine(packageRoot, "tests", "hidden", "hidden.json"),
            """{"plan":{"mustContain":[42]}}""");

        var exception = Assert.Throws<ChallengePackageValidationException>(
            () => new ChallengePackageLoader(new ChallengeLocation(root.Path)).Load());

        Assert.Contains("plan.mustContain[0]", exception.Message);
    }

    [Fact]
    public void Load_SqlTestWithInvalidExpectedRow_Throws()
    {
        using var root = new TemporaryDirectory();
        var packageRoot = ChallengeTestContent.AddSqlPackage(root.Path);
        File.WriteAllText(
            Path.Combine(packageRoot, "tests", "hidden", "hidden.json"),
            """{"expected":[[{"nested":true}]]}""");

        var exception = Assert.Throws<ChallengePackageValidationException>(
            () => new ChallengePackageLoader(new ChallengeLocation(root.Path)).Load());

        Assert.Contains("must be a string, number, boolean, or null", exception.Message);
    }
}
