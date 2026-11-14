using System.Text.Json;
using Psel.Modules.Challenges.Packages;

namespace Psel.Api.Tests;

public sealed class ChallengeHarnessTests
{
    private const string HarnessSentinel = "HARNESS_SOURCE_SENTINEL";

    [Fact]
    public void LoadPackages_KeepsHarnessOutOfClientProjection()
    {
        using var root = new TemporaryDirectory();
        var packageRoot = ChallengeTestContent.AddPackage(
            root.Path,
            WithHarness("harness/PselHarness.cs"));
        Write(packageRoot, "harness/PselHarness.cs", $"// {HarnessSentinel}");

        var packages = new ChallengePackageLoader(new ChallengeLocation(root.Path)).LoadPackages();
        var detail = Assert.Single(packages.Details);

        Assert.True(Assert.Single(detail.Languages).CanExecute);
        Assert.Equal(
            $"// {HarnessSentinel}",
            packages.Harnesses["sample-challenge"]["csharp"]);

        var serialized = JsonSerializer.Serialize(detail);
        Assert.DoesNotContain(HarnessSentinel, serialized, StringComparison.Ordinal);
    }

    [Fact]
    public void LoadPackages_WithoutHarness_MarksLanguageNotExecutable()
    {
        using var root = new TemporaryDirectory();
        ChallengeTestContent.AddPackage(root.Path);

        var packages = new ChallengePackageLoader(new ChallengeLocation(root.Path)).LoadPackages();

        Assert.False(Assert.Single(Assert.Single(packages.Details).Languages).CanExecute);
        Assert.Empty(packages.Harnesses["sample-challenge"]);
    }

    [Fact]
    public void LoadPackages_MissingHarnessFile_Throws()
    {
        using var root = new TemporaryDirectory();
        ChallengeTestContent.AddPackage(root.Path, WithHarness("harness/Missing.cs"));

        var exception = Assert.Throws<ChallengePackageValidationException>(
            () => new ChallengePackageLoader(new ChallengeLocation(root.Path)).LoadPackages());

        Assert.Contains("points to a missing file", exception.Message);
    }

    [Fact]
    public void LoadPackages_HarnessOutsidePackage_Throws()
    {
        using var root = new TemporaryDirectory();
        ChallengeTestContent.AddPackage(root.Path, WithHarness("../../escape.cs"));

        var exception = Assert.Throws<ChallengePackageValidationException>(
            () => new ChallengePackageLoader(new ChallengeLocation(root.Path)).LoadPackages());

        Assert.Contains(
            "must be a relative path inside the package",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LoadPackages_EmptyHarness_Throws()
    {
        using var root = new TemporaryDirectory();
        var packageRoot = ChallengeTestContent.AddPackage(
            root.Path,
            WithHarness("harness/PselHarness.cs"));
        Write(packageRoot, "harness/PselHarness.cs", "   ");

        var exception = Assert.Throws<ChallengePackageValidationException>(
            () => new ChallengePackageLoader(new ChallengeLocation(root.Path)).LoadPackages());

        Assert.Contains("must not be empty", exception.Message);
    }

    [Fact]
    public void LoadPackages_RepositoryPackages_ProvidePythonHarnesses()
    {
        var packages = new ChallengePackageLoader(
                new ChallengeLocation(ChallengeTestPaths.RepositoryChallengePath()))
            .LoadPackages();

        var pythonLanguages = packages.Details
            .SelectMany(detail => detail.Languages
                .Where(language => language.Id == "python")
                .Select(language => (detail.Slug, Language: language)))
            .ToList();

        Assert.NotEmpty(pythonLanguages);

        Assert.All(
            pythonLanguages,
            entry =>
            {
                Assert.True(entry.Language.CanExecute);
                Assert.Contains(
                    "import solution",
                    packages.Harnesses[entry.Slug]["python"],
                    StringComparison.Ordinal);
            });
    }

    private static string WithHarness(string path)
    {
        return ChallengeTestContent.MinimalManifest.Replace(
            "\"editorLanguage\": \"csharp\",",
            $"\"editorLanguage\": \"csharp\", \"harness\": \"{path}\",",
            StringComparison.Ordinal);
    }

    private static void Write(string root, string relativePath, string content)
    {
        var fullPath = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, content);
    }
}
