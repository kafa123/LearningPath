using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Psel.Modules.Challenges.Contracts;

namespace Psel.Api.Tests;

public sealed class ChallengeAuthoringEndpointTests
{
    private const string SourceSlug = "balanced-brackets";

    [Fact]
    public async Task Upload_InstallsNewPackageAndReloadsCatalog()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var challenges = new TempChallengeRoot();
        using var factory = new CustomWebApplicationFactory(challenges.Path);
        using var client = factory.CreateClient();

        var archive = BuildArchive(
            CopyPackage(),
            manifest =>
            {
                manifest["slug"] = "balanced-brackets-copy";
                manifest["title"] = "Balanced Brackets Copy";
            });

        using var response = await UploadAsync(client, archive, cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ChallengePackageImportResult>(
            cancellationToken);
        Assert.NotNull(result);
        Assert.Equal("balanced-brackets-copy", result.Slug);
        Assert.Equal("Balanced Brackets Copy", result.Title);
        Assert.Equal("programming-fundamentals", result.DomainSlug);
        Assert.False(result.Replaced);
        Assert.Equal(2, result.ChallengeCount);

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        Assert.DoesNotContain(challenges.Path, body, StringComparison.OrdinalIgnoreCase);

        var installed = Path.Combine(
            challenges.Path,
            "programming-fundamentals",
            "balanced-brackets-copy",
            "challenge.json");
        Assert.True(File.Exists(installed));

        using var detailResponse = await client.GetAsync(
            "/api/challenges/balanced-brackets-copy",
            cancellationToken);
        detailResponse.EnsureSuccessStatusCode();
        var detailJson = await detailResponse.Content.ReadAsStringAsync(cancellationToken);

        Assert.Contains("Balanced Brackets Copy", detailJson, StringComparison.Ordinal);
        Assert.DoesNotContain("PselHarness", detailJson, StringComparison.Ordinal);
        Assert.DoesNotContain("unclosed.json", detailJson, StringComparison.Ordinal);
        Assert.DoesNotContain("interleaved-text", detailJson, StringComparison.Ordinal);
        Assert.DoesNotContain("solutions/", detailJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(challenges.Path, detailJson, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Upload_RejectsExistingPackageUnlessReplacementIsRequested()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var challenges = new TempChallengeRoot();
        using var factory = new CustomWebApplicationFactory(challenges.Path);
        using var client = factory.CreateClient();

        var archive = BuildArchive(
            CopyPackage(),
            manifest => manifest["title"] = "Balanced Brackets Replaced");

        using var conflict = await UploadAsync(client, archive, cancellationToken);
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);

        var conflictBody = await conflict.Content.ReadAsStringAsync(cancellationToken);
        Assert.Contains("replacement", conflictBody, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(
            "Balanced Brackets",
            await ReadTitleAsync(client, SourceSlug, cancellationToken));

        using var replaced = await UploadAsync(
            client,
            archive,
            cancellationToken,
            replace: true);
        Assert.Equal(HttpStatusCode.OK, replaced.StatusCode);

        var result = await replaced.Content.ReadFromJsonAsync<ChallengePackageImportResult>(
            cancellationToken);
        Assert.NotNull(result);
        Assert.True(result.Replaced);
        Assert.Equal(1, result.ChallengeCount);
        Assert.Equal(
            "Balanced Brackets Replaced",
            await ReadTitleAsync(client, SourceSlug, cancellationToken));
    }

    [Fact]
    public async Task Upload_RejectsZipSlipArchiveWithoutWritingOutsideTheRoot()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var challenges = new TempChallengeRoot();
        using var factory = new CustomWebApplicationFactory(challenges.Path);
        using var client = factory.CreateClient();

        using var buffer = new MemoryStream();

        using (var archive = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entry = archive.CreateEntry("../escaped.json");
            await using var writer = new StreamWriter(entry.Open());
            await writer.WriteAsync("{}");
        }

        using var response = await UploadAsync(client, buffer.ToArray(), cancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        Assert.Contains("unsafe", body, StringComparison.OrdinalIgnoreCase);
        Assert.False(File.Exists(Path.Combine(challenges.Path, "escaped.json")));
    }

    [Fact]
    public async Task Upload_RejectsArchivesThatAreNotOnePackage()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var challenges = new TempChallengeRoot();
        using var factory = new CustomWebApplicationFactory(challenges.Path);
        using var client = factory.CreateClient();

        var missingManifest = BuildZipFromEntries(("notes/readme.md", "nothing to install"));

        using var missingResponse = await UploadAsync(
            client,
            missingManifest,
            cancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, missingResponse.StatusCode);

        var duplicated = BuildZipFromEntries(
            ("one/challenge.json", "{}"),
            ("two/challenge.json", "{}"));

        using var duplicateResponse = await UploadAsync(
            client,
            duplicated,
            cancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, duplicateResponse.StatusCode);
        var duplicateBody = await duplicateResponse.Content.ReadAsStringAsync(cancellationToken);
        Assert.Contains("exactly one", duplicateBody, StringComparison.OrdinalIgnoreCase);

        using var invalidResponse = await UploadAsync(
            client,
            "this is not a zip archive"u8.ToArray(),
            cancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, invalidResponse.StatusCode);
    }

    private static async Task<HttpResponseMessage> UploadAsync(
        HttpClient client,
        byte[] archive,
        CancellationToken cancellationToken,
        bool replace = false)
    {
        using var content = new MultipartFormDataContent();
        using var file = new ByteArrayContent(archive);
        file.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
        content.Add(file, "file", "package.zip");

        var path = replace
            ? "/api/authoring/challenge-packages?replace=true"
            : "/api/authoring/challenge-packages";

        return await client.PostAsync(path, content, cancellationToken);
    }

    private static async Task<string> ReadTitleAsync(
        HttpClient client,
        string slug,
        CancellationToken cancellationToken)
    {
        using var response = await client.GetAsync($"/api/challenges/{slug}", cancellationToken);
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync(cancellationToken));

        return document.RootElement.GetProperty("title").GetString()!;
    }

    private static string CopyPackage()
    {
        var source = FindSourcePackage();
        var destination = Path.Combine(
            Path.GetTempPath(),
            $"psel-authoring-source-{Guid.NewGuid():N}");

        CopyDirectory(source, destination);
        return destination;
    }

    private static string FindSourcePackage()
    {
        var match = Directory
            .EnumerateDirectories(
                ChallengeTestPaths.RepositoryChallengePath(),
                SourceSlug,
                SearchOption.AllDirectories)
            .FirstOrDefault(directory =>
                File.Exists(Path.Combine(directory, "challenge.json")));

        return match ?? throw new InvalidOperationException(
            $"Could not find the '{SourceSlug}' challenge package.");
    }

    private static byte[] BuildArchive(string packageRoot, Action<JsonObject> mutateManifest)
    {
        var manifestPath = Path.Combine(packageRoot, "challenge.json");
        var manifest = JsonNode.Parse(File.ReadAllText(manifestPath))!.AsObject();
        mutateManifest(manifest);
        File.WriteAllText(
            manifestPath,
            manifest.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));

        using var buffer = new MemoryStream();

        using (var archive = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var file in Directory.EnumerateFiles(
                packageRoot,
                "*",
                SearchOption.AllDirectories))
            {
                var entryName = Path
                    .GetRelativePath(packageRoot, file)
                    .Replace(Path.DirectorySeparatorChar, '/');
                archive.CreateEntryFromFile(file, entryName);
            }
        }

        TryDeleteDirectory(packageRoot);
        return buffer.ToArray();
    }

    private static byte[] BuildZipFromEntries(params (string Name, string Content)[] entries)
    {
        using var buffer = new MemoryStream();

        using (var archive = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (name, content) in entries)
            {
                var entry = archive.CreateEntry(name);
                using var writer = new StreamWriter(entry.Open());
                writer.Write(content);
            }
        }

        return buffer.ToArray();
    }

    private static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);

        foreach (var directory in Directory.EnumerateDirectories(
            source,
            "*",
            SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(
                Path.Combine(destination, Path.GetRelativePath(source, directory)));
        }

        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            File.Copy(
                file,
                Path.Combine(destination, Path.GetRelativePath(source, file)),
                overwrite: true);
        }
    }

    private static void TryDeleteDirectory(string path)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, recursive: true);
                }

                return;
            }
            catch (IOException) when (attempt < 4)
            {
                Thread.Sleep(50);
            }
            catch (UnauthorizedAccessException) when (attempt < 4)
            {
                Thread.Sleep(50);
            }
        }
    }

    /// <summary>
    /// Isolated challenge root that starts with one repository package, so the
    /// catalog under test has known content and uploads never touch the repo.
    /// </summary>
    private sealed class TempChallengeRoot : IDisposable
    {
        public TempChallengeRoot()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                $"psel-challenge-root-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);

            CopyDirectory(
                FindSourcePackage(),
                System.IO.Path.Combine(Path, "programming-fundamentals", SourceSlug));
        }

        public string Path { get; }

        public void Dispose()
        {
            TryDeleteDirectory(Path);
        }
    }
}
