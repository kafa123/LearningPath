namespace Psel.Api.Tests;

public sealed class ChallengeStartupTests
{
    [Fact]
    public void Startup_InvalidPackage_FailsFastWithValidationError()
    {
        using var root = new TemporaryDirectory();
        ChallengeTestContent.AddPackage(
            root.Path,
            ChallengeTestContent.MinimalManifest.Replace(
                "\"schemaVersion\": 1,",
                "\"schemaVersion\": 2,",
                StringComparison.Ordinal));

        using var factory = new CustomWebApplicationFactory(root.Path);

        var exception = Record.Exception(() => factory.CreateClient());

        Assert.NotNull(exception);
        Assert.Contains(
            "schemaVersion must be 1",
            exception.ToString(),
            StringComparison.Ordinal);
    }

    [Fact]
    public void Startup_EmptyChallengeDirectory_FailsFast()
    {
        using var root = new TemporaryDirectory();
        using var factory = new CustomWebApplicationFactory(root.Path);

        var exception = Record.Exception(() => factory.CreateClient());

        Assert.NotNull(exception);
        Assert.Contains(
            "No challenge packages were found",
            exception.ToString(),
            StringComparison.OrdinalIgnoreCase);
    }
}
