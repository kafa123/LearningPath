using System.IO.Compression;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Psel.Modules.Challenges.Contracts;

namespace Psel.Modules.Challenges.Packages;

internal sealed class ChallengePackageImportException : Exception
{
    public ChallengePackageImportException(int statusCode, string error)
        : this(statusCode, [error])
    {
    }

    public ChallengePackageImportException(int statusCode, IReadOnlyList<string> errors)
        : base(string.Join(" | ", errors))
    {
        StatusCode = statusCode;
        Errors = errors;
    }

    public int StatusCode { get; }

    public IReadOnlyList<string> Errors { get; }
}

/// <summary>
/// Installs one locally uploaded challenge package. The archive is extracted to
/// a temporary directory and validated by the same loader used at startup
/// before any file inside the challenge root is touched.
/// </summary>
internal sealed class ChallengePackageImporter(
    ChallengeLocation location,
    ChallengeCatalog catalog,
    ILogger<ChallengePackageImporter> logger)
{
    private const long MaxUploadBytes = 25 * 1024 * 1024;
    private const long MaxUncompressedBytes = 50 * 1024 * 1024;
    private const long MaxFileBytes = 5 * 1024 * 1024;
    private const int MaxEntries = 1000;

    public async Task<ChallengePackageImportResult> ImportAsync(
        IFormFile file,
        bool replace,
        CancellationToken cancellationToken)
    {
        if (file.Length <= 0)
        {
            throw Rejected("The uploaded archive is empty.");
        }

        if (file.Length > MaxUploadBytes)
        {
            throw Rejected(
                $"Challenge archives must be at most {MaxUploadBytes / (1024 * 1024)} MiB.");
        }

        if (!string.Equals(Path.GetExtension(file.FileName), ".zip", StringComparison.OrdinalIgnoreCase))
        {
            throw Rejected("Upload the challenge package as a .zip archive.");
        }

        var tempRoot = Path.Combine(
            Path.GetTempPath(),
            $"psel-challenge-upload-{Guid.NewGuid():N}");
        var backupRoot = $"{tempRoot}-backup";
        Directory.CreateDirectory(tempRoot);

        try
        {
            await ExtractAsync(file, tempRoot, cancellationToken);

            var manifests = Directory
                .EnumerateFiles(tempRoot, "challenge.json", SearchOption.AllDirectories)
                .ToList();

            if (manifests.Count == 0)
            {
                throw Rejected(
                    "The archive does not contain challenge.json in its root or in one package folder.");
            }

            if (manifests.Count > 1)
            {
                throw Rejected(
                    "The archive must contain exactly one challenge package, "
                    + $"but {manifests.Count} challenge.json files were found.");
            }

            var packageRoot = Path.GetDirectoryName(manifests[0])!;
            ChallengePackageSet validated;

            try
            {
                validated = new ChallengePackageLoader(new ChallengeLocation(packageRoot))
                    .LoadPackages();
            }
            catch (ChallengePackageValidationException exception)
            {
                throw new ChallengePackageImportException(
                    StatusCodes.Status400BadRequest,
                    exception.Errors);
            }

            if (validated.Details.Count != 1)
            {
                throw Rejected("The archive must contain exactly one challenge package.");
            }

            var detail = validated.Details[0];
            var target = Path.GetFullPath(
                Path.Combine(location.Path, detail.DomainSlug, detail.Slug));
            var root = Path.GetFullPath(location.Path);

            if (!target.StartsWith(
                root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar,
                StringComparison.OrdinalIgnoreCase))
            {
                throw Rejected("The package path is outside the challenge directory.");
            }

            var targetExists = Directory.Exists(target);
            var existing = catalog.FindBySlug(detail.Slug);

            if (existing is not null && !targetExists)
            {
                throw new ChallengePackageImportException(
                    StatusCodes.Status409Conflict,
                    $"A challenge with slug '{detail.Slug}' already exists in another package folder.");
            }

            if (targetExists && !replace)
            {
                throw new ChallengePackageImportException(
                    StatusCodes.Status409Conflict,
                    $"A package already exists for '{detail.Slug}'. "
                    + "Enable replacement to overwrite it.");
            }

            var backup = targetExists
                ? Path.Combine(backupRoot, "package")
                : null;

            if (backup is not null)
            {
                CopyDirectory(target, backup);
            }

            try
            {
                if (targetExists)
                {
                    Directory.Delete(target, recursive: true);
                }

                CopyDirectory(packageRoot, target);
                catalog.Reload();
            }
            catch
            {
                if (Directory.Exists(target))
                {
                    Directory.Delete(target, recursive: true);
                }

                if (backup is not null && Directory.Exists(backup))
                {
                    CopyDirectory(backup, target);
                }

                TryReload();

                throw;
            }

            var count = catalog.EnsureLoaded();

            logger.LogInformation(
                "Installed challenge package {Slug} ({Replaced}) from an upload. Catalog size: {Count}.",
                detail.Slug,
                targetExists ? "replaced" : "new",
                count);

            return new ChallengePackageImportResult(
                detail.Slug,
                detail.Title,
                detail.DomainSlug,
                count,
                targetExists);
        }
        catch (InvalidDataException)
        {
            throw Rejected("The uploaded file is not a valid ZIP archive.");
        }
        catch (IOException exception)
        {
            throw new ChallengePackageImportException(
                StatusCodes.Status500InternalServerError,
                $"The challenge package could not be installed: {exception.Message}");
        }
        finally
        {
            TryDeleteDirectory(tempRoot);
            TryDeleteDirectory(backupRoot);
        }
    }

    private static async Task ExtractAsync(
        IFormFile file,
        string destination,
        CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

        if (archive.Entries.Count > MaxEntries)
        {
            throw Rejected($"The archive must contain at most {MaxEntries} entries.");
        }

        var root = Path.GetFullPath(destination);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        long totalLength = 0;

        foreach (var entry in archive.Entries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var name = entry.FullName.Replace('\\', '/');

            if (string.IsNullOrWhiteSpace(name) || name.EndsWith('/'))
            {
                continue;
            }

            if (name.Contains('\0')
                || Path.IsPathRooted(name)
                || name.StartsWith('/')
                || name.Contains(':', StringComparison.Ordinal))
            {
                throw Rejected($"The archive contains an unsafe path: '{name}'.");
            }

            var segments = name.Split('/', StringSplitOptions.RemoveEmptyEntries);

            if (segments.Length == 0
                || segments.Any(segment => segment is "." or ".."))
            {
                throw Rejected($"The archive contains an unsafe path: '{name}'.");
            }

            if (string.Equals(segments[0], "__MACOSX", StringComparison.OrdinalIgnoreCase)
                || string.Equals(segments[^1], ".DS_Store", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var unixMode = (entry.ExternalAttributes >> 16) & 0xF000;

            if (unixMode == 0xA000)
            {
                throw Rejected($"The archive contains a symbolic link: '{name}'.");
            }

            if (entry.Length > MaxFileBytes)
            {
                throw Rejected($"The archive entry '{name}' is larger than 5 MiB.");
            }

            totalLength += entry.Length;

            if (totalLength > MaxUncompressedBytes)
            {
                throw Rejected("The archive expands to more than 50 MiB.");
            }

            var relativePath = Path.Combine(segments);
            var path = Path.GetFullPath(Path.Combine(root, relativePath));

            if (!path.StartsWith(
                root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar,
                StringComparison.OrdinalIgnoreCase)
                || !seen.Add(path))
            {
                throw Rejected($"The archive contains an unsafe or duplicate path: '{name}'.");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(path)!);

            await using var output = new FileStream(
                path,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None);
            using var entryStream = entry.Open();
            await entryStream.CopyToAsync(output, cancellationToken);
        }
    }

    private static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);

        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(source, file);
            var target = Path.Combine(destination, relativePath);

            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target, overwrite: true);
        }
    }

    private void TryReload()
    {
        try
        {
            catalog.Reload();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "The challenge catalog could not be reloaded after a rollback.");
        }
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch (IOException)
        {
            // Temporary content is best-effort cleanup.
        }
        catch (UnauthorizedAccessException)
        {
            // Temporary content is best-effort cleanup.
        }
    }

    private static ChallengePackageImportException Rejected(string error)
    {
        return new ChallengePackageImportException(
            StatusCodes.Status400BadRequest,
            error);
    }
}
