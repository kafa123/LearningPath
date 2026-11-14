using Psel.Infrastructure.Persistence;

namespace Psel.Api.Tests;

public sealed class DatabasePathResolverTests
{
    [Fact]
    public void Resolve_RelativePath_IsCombinedWithContentRoot()
    {
        var contentRoot = Path.Combine(Path.GetTempPath(), "psel-root");

        var result = DatabasePathResolver.Resolve("data/psel.db", contentRoot);

        Assert.Equal(
            Path.GetFullPath(Path.Combine(contentRoot, "data/psel.db")),
            result);
    }

    [Fact]
    public void Resolve_AbsolutePath_IsPreserved()
    {
        var absolutePath = Path.Combine(Path.GetTempPath(), "psel-absolute.db");

        var result = DatabasePathResolver.Resolve(absolutePath, Path.GetTempPath());

        Assert.Equal(Path.GetFullPath(absolutePath), result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Resolve_BlankPath_UsesDefaultRelativePath(string? configuredPath)
    {
        var contentRoot = Path.Combine(Path.GetTempPath(), "psel-root");

        var result = DatabasePathResolver.Resolve(configuredPath, contentRoot);

        Assert.Equal(
            Path.GetFullPath(Path.Combine(contentRoot, DatabasePathResolver.DefaultRelativePath)),
            result);
    }
}
