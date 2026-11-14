using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Psel.Infrastructure.Persistence;

namespace Psel.Api.Tests;

/// <summary>
/// Starts the API with an isolated temporary SQLite database and challenge
/// package root for each test class.
/// </summary>
public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string databasePath = Path.Combine(
        Path.GetTempPath(),
        $"psel-tests-{Guid.NewGuid():N}.db");

    private readonly string challengePath;

    public CustomWebApplicationFactory()
        : this(ChallengeTestPaths.RepositoryChallengePath())
    {
    }

    public CustomWebApplicationFactory(string challengePath)
    {
        this.challengePath = challengePath;
    }

    public string DatabasePath => databasePath;

    public FakeLanguageRunner Runner { get; } = new();

    public FakeLanguageRunner PythonRunner { get; } = new("python");

    public FakeLanguageRunner SqlRunner { get; } = new("sql");

    public TestTimeProvider Clock { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Challenges:Path"] = challengePath,
                    ["Execution:Enabled"] = "false"
                });
        });
        builder.ConfigureLogging(logging =>
        {
            logging.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);
            logging.AddFilter("Microsoft.Hosting.Lifetime", LogLevel.Warning);
        });
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<PselDbContext>>();
            services.RemoveAll<PselDbContext>();
            services.RemoveAll<DatabaseLocation>();

            services.AddSingleton(new DatabaseLocation(databasePath));
            services.AddDbContext<PselDbContext>(options =>
                options.UseSqlite($"Data Source={databasePath}"));

            services.RemoveAll<Psel.Modules.Execution.ILanguageRunner>();
            services.AddSingleton<Psel.Modules.Execution.ILanguageRunner>(Runner);
            services.AddSingleton<Psel.Modules.Execution.ILanguageRunner>(PythonRunner);
            services.AddSingleton<Psel.Modules.Execution.ILanguageRunner>(SqlRunner);

            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Clock);
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
        {
            return;
        }

        // Microsoft.Data.Sqlite pools connections. Clear them before deleting
        // the temporary database so the file handle is released.
        SqliteConnection.ClearAllPools();

        DeleteDatabaseFile(databasePath);
        DeleteDatabaseFile($"{databasePath}-shm");
        DeleteDatabaseFile($"{databasePath}-wal");
        DeleteDatabaseFile($"{databasePath}-journal");
    }

    private static void DeleteDatabaseFile(string path)
    {
        if (!File.Exists(path))
        {
            return;
        }

        for (var attempt = 0; attempt < 5; attempt++)
        {
            try
            {
                File.Delete(path);
                return;
            }
            catch (IOException) when (attempt < 4)
            {
                Thread.Sleep(50);
            }
        }
    }
}

internal static class ChallengeTestPaths
{
    public static string RepositoryChallengePath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "challenges");

            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException(
            "Could not locate the repository challenges directory.");
    }
}
