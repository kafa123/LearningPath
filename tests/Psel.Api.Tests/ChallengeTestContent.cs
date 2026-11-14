namespace Psel.Api.Tests;

/// <summary>
/// Builds disposable challenge package trees for loader and startup tests.
/// </summary>
internal static class ChallengeTestContent
{
    public const string HiddenSentinel = "HIDDEN_TEST_SENTINEL";
    public const string HiddenSqlSentinel = "HIDDEN_SQL_SENTINEL";
    public const string SolutionSentinel = "SOLUTION_SENTINEL";
    public const string SqlSchemaSentinel = "SQL_SCHEMA_SENTINEL";
    public const string SqlSeedSentinel = "SQL_SEED_SENTINEL";

    public const string MinimalManifest = """
        {
          "schemaVersion": 1,
          "slug": "sample-challenge",
          "title": "Sample Challenge",
          "summary": "A sample challenge used by tests.",
          "domainSlug": "programming-fundamentals",
          "topic": "Testing",
          "difficulty": "Beginner",
          "type": "Coding",
          "estimatedMinutes": 15,
          "objectives": ["Write a valid package."],
          "prerequisites": [],
          "constraints": ["Keep it small."],
          "examples": [
            {
              "input": "1",
              "output": "1"
            }
          ],
          "languages": [
            {
              "id": "csharp",
              "name": "C#",
              "editorLanguage": "csharp",
              "starterFiles": [
                {
                  "path": "Solution.cs",
                  "source": "starter/Solution.cs"
                }
              ]
            }
          ],
          "statement": "statement.md",
          "hints": "hints.md",
          "tests": {
            "public": [
              {
                "name": "One",
                "source": "tests/public/one.json"
              }
            ],
            "hidden": [
              {
                "name": "Hidden",
                "source": "tests/hidden/hidden.json"
              }
            ]
          },
          "evaluation": {
            "kind": "AutomatedTests",
            "timeLimitSeconds": 5,
            "memoryLimitMb": 256
          }
        }
        """;

    public const string MinimalSqlManifest = """
        {
          "schemaVersion": 1,
          "slug": "sample-sql",
          "title": "Sample SQL Challenge",
          "summary": "A sample SQL challenge used by tests.",
          "domainSlug": "database-engineering",
          "topic": "Testing",
          "difficulty": "Intermediate",
          "type": "SQL",
          "estimatedMinutes": 20,
          "objectives": ["Write a valid SQL package."],
          "prerequisites": [],
          "constraints": ["Use a read-only query."],
          "examples": [
            {
              "input": "one row",
              "output": "value = 1"
            }
          ],
          "languages": [
            {
              "id": "sql",
              "name": "SQL",
              "editorLanguage": "sql",
              "starterFiles": [
                {
                  "path": "query.sql",
                  "source": "starter/sql/query.sql"
                }
              ]
            }
          ],
          "statement": "statement.md",
          "hints": "hints.md",
          "sql": {
            "schema": "sql/schema.sql",
            "seed": "sql/seed.sql"
          },
          "tests": {
            "public": [
              {
                "name": "One",
                "source": "tests/public/one.json"
              }
            ],
            "hidden": [
              {
                "name": "Hidden",
                "source": "tests/hidden/hidden.json"
              }
            ]
          },
          "evaluation": {
            "kind": "AutomatedTests",
            "timeLimitSeconds": 5,
            "memoryLimitMb": 256
          }
        }
        """;

    public static string AddSqlPackage(
        string repositoryRoot,
        string? manifest = null,
        string packageName = "sample-sql")
    {
        var packageRoot = Path.Combine(repositoryRoot, packageName);
        Directory.CreateDirectory(packageRoot);

        Write(packageRoot, "challenge.json", manifest ?? MinimalSqlManifest);
        Write(packageRoot, "statement.md", "# Sample SQL statement");
        Write(packageRoot, "hints.md", "# Sample SQL hints");
        Write(packageRoot, "starter/sql/query.sql", "SELECT 1 AS value;");
        Write(packageRoot, "sql/schema.sql", $"CREATE TABLE sample (value INTEGER); -- {SqlSchemaSentinel}");
        Write(packageRoot, "sql/seed.sql", $"INSERT INTO sample VALUES (1); -- {SqlSeedSentinel}");
        Write(packageRoot, "tests/public/one.json", """{"expected":[[1]]}""");
        Write(
            packageRoot,
            "tests/hidden/hidden.json",
            $$"""{"expected":[["{{HiddenSqlSentinel}}"]]}""");
        Write(packageRoot, "solutions/sql/query.sql", SolutionSentinel);

        return packageRoot;
    }

    public static string AddPackage(
        string repositoryRoot,
        string? manifest = null,
        string packageName = "sample")
    {
        var packageRoot = Path.Combine(repositoryRoot, packageName);
        Directory.CreateDirectory(packageRoot);

        Write(packageRoot, "challenge.json", manifest ?? MinimalManifest);
        Write(packageRoot, "statement.md", "# Sample statement");
        Write(packageRoot, "hints.md", "# Sample hints");
        Write(packageRoot, "starter/Solution.cs", "// starter marker");
        Write(packageRoot, "tests/public/one.json", """{"input":{"value":1},"expected":1}""");
        Write(
            packageRoot,
            "tests/hidden/hidden.json",
            $$"""{"input":{"value":"{{HiddenSentinel}}"},"expected":"{{HiddenSentinel}}"}""");
        Write(packageRoot, "solutions/Solution.cs", SolutionSentinel);

        return packageRoot;
    }

    private static void Write(string root, string relativePath, string content)
    {
        var fullPath = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, content);
    }
}

internal sealed class TemporaryDirectory : IDisposable
{
    public TemporaryDirectory()
    {
        Path = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            $"psel-challenge-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public void Dispose()
    {
        if (!Directory.Exists(Path))
        {
            return;
        }

        for (var attempt = 0; attempt < 5; attempt++)
        {
            try
            {
                Directory.Delete(Path, recursive: true);
                return;
            }
            catch (IOException) when (attempt < 4)
            {
                Thread.Sleep(50);
            }
        }
    }
}
