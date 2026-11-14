using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Psel.Modules.Execution;
using Psel.Modules.Execution.Docker;
using Psel.Modules.Execution.Runners;

namespace Psel.Api.Tests;

public sealed class SqlDockerRunnerTests
{
    [Fact]
    public async Task ExecuteAsync_WritesTrustedWorkspaceAndMapsSuccess()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var root = new TemporaryDirectory();
        var settings = CreateSettings(root.Path);
        var engine = new RecordingDockerEngine
        {
            Result = new DockerRunResult(
                0,
                "__PSEL_RESULTS_BEGIN__\n[]\n__PSEL_RESULTS_END__\n",
                string.Empty,
                18,
                false,
                false,
                false,
                false,
                false,
                null)
        };
        var runner = CreateRunner(engine, settings);
        var submissionId = Guid.NewGuid();

        var outcome = await runner.ExecuteAsync(
            new ExecutionRequest(
                submissionId,
                "order-revenue",
                "sql",
                [new ExecutionFile("query.sql", "SELECT 1;")],
                "# SERVER_SQL_HARNESS",
                [
                    new ExecutionTest(
                        "public-1",
                        "public",
                        """{"schema":"CREATE TABLE t (id INTEGER);","seed":"INSERT INTO t VALUES (1);","expected":[[1]]}""")
                ]),
            cancellationToken);

        Assert.Equal(ExecutionStatus.Succeeded, outcome.Status);
        Assert.Equal(0, outcome.ExitCode);

        var request = Assert.IsType<DockerRunRequest>(engine.Request);
        Assert.Equal(settings.SqlImage, request.Image);
        Assert.Equal($"psel-exec-{submissionId:N}", request.ContainerName);
        Assert.Contains("python3 -B /workspace/PselHarness.py", request.Command, StringComparison.Ordinal);
        Assert.Equal("/tmp", request.EnvironmentVariables["HOME"]);

        Assert.Equal("# SERVER_SQL_HARNESS", engine.Files["PselHarness.py"]);
        Assert.Equal("SELECT 1;", engine.Files["query.sql"]);
        Assert.Contains("\"id\":\"public-1\"", engine.Files["PselTests.json"], StringComparison.Ordinal);
        Assert.Contains("\"schema\":\"CREATE TABLE t", engine.Files["PselTests.json"], StringComparison.Ordinal);
        Assert.True(engine.WorkspaceExistedDuringRun);

        Assert.False(Directory.Exists(request.WorkspacePath));
    }

    [Fact]
    public async Task ExecuteAsync_WithoutHarness_ReturnsExecutionUnavailable()
    {
        using var root = new TemporaryDirectory();
        var engine = new RecordingDockerEngine();
        var runner = CreateRunner(engine, CreateSettings(root.Path));

        var outcome = await runner.ExecuteAsync(
            CreateRequest() with { HarnessSource = null },
            TestContext.Current.CancellationToken);

        Assert.Equal(ExecutionStatus.ExecutionUnavailable, outcome.Status);
        Assert.Null(engine.Request);
    }

    [Fact]
    public async Task ExecuteAsync_WithoutTests_ReturnsExecutionUnavailable()
    {
        using var root = new TemporaryDirectory();
        var engine = new RecordingDockerEngine();
        var runner = CreateRunner(engine, CreateSettings(root.Path));

        var outcome = await runner.ExecuteAsync(
            CreateRequest() with { Tests = null },
            TestContext.Current.CancellationToken);

        Assert.Equal(ExecutionStatus.ExecutionUnavailable, outcome.Status);
        Assert.Null(engine.Request);
    }

    [Fact]
    public async Task ExecuteAsync_MultipleFiles_RejectsBeforeDocker()
    {
        using var root = new TemporaryDirectory();
        var engine = new RecordingDockerEngine();
        var runner = CreateRunner(engine, CreateSettings(root.Path));

        var outcome = await runner.ExecuteAsync(
            CreateRequest() with
            {
                Files =
                [
                    new ExecutionFile("query.sql", "SELECT 1;"),
                    new ExecutionFile("extra.sql", "SELECT 2;")
                ]
            },
            TestContext.Current.CancellationToken);

        Assert.Equal(ExecutionStatus.InternalError, outcome.Status);
        Assert.Null(engine.Request);
    }

    [Theory]
    [InlineData("PselHarness.py")]
    [InlineData("PselTests.json")]
    [InlineData("../escape.sql")]
    [InlineData("/absolute.sql")]
    public async Task ExecuteAsync_UnsafePath_RejectsBeforeDocker(string path)
    {
        using var root = new TemporaryDirectory();
        var engine = new RecordingDockerEngine();
        var runner = CreateRunner(engine, CreateSettings(root.Path));

        var outcome = await runner.ExecuteAsync(
            CreateRequest() with { Files = [new ExecutionFile(path, "SELECT 1;")] },
            TestContext.Current.CancellationToken);

        Assert.Equal(ExecutionStatus.InternalError, outcome.Status);
        Assert.Null(engine.Request);
    }

    [Fact]
    public async Task ExecuteAsync_NonSqlFile_RejectsBeforeDocker()
    {
        using var root = new TemporaryDirectory();
        var engine = new RecordingDockerEngine();
        var runner = CreateRunner(engine, CreateSettings(root.Path));

        var outcome = await runner.ExecuteAsync(
            CreateRequest() with { Files = [new ExecutionFile("query.py", "SELECT 1")] },
            TestContext.Current.CancellationToken);

        Assert.Equal(ExecutionStatus.InternalError, outcome.Status);
        Assert.Null(engine.Request);
    }

    [Fact]
    public async Task ExecuteAsync_WrongLanguage_ReturnsInternalError()
    {
        using var root = new TemporaryDirectory();
        var engine = new RecordingDockerEngine();
        var runner = CreateRunner(engine, CreateSettings(root.Path));

        var outcome = await runner.ExecuteAsync(
            CreateRequest() with { LanguageId = "python" },
            TestContext.Current.CancellationToken);

        Assert.Equal(ExecutionStatus.InternalError, outcome.Status);
        Assert.Null(engine.Request);
    }

    [Fact]
    public async Task ExecuteAsync_SqlCompileMarkerMapsToCompilationError()
    {
        using var root = new TemporaryDirectory();
        var engine = new RecordingDockerEngine
        {
            Result = new DockerRunResult(
                1,
                string.Empty,
                "PSEL_SQL_COMPILE_ERROR\npublic-1: near \"SELEC\": syntax error",
                90,
                false,
                false,
                false,
                false,
                false,
                null)
        };
        var runner = CreateRunner(engine, CreateSettings(root.Path));

        var outcome = await runner.ExecuteAsync(
            CreateRequest(),
            TestContext.Current.CancellationToken);

        Assert.Equal(ExecutionStatus.CompilationError, outcome.Status);
        Assert.Contains("PSEL_SQL_COMPILE_ERROR", outcome.StandardError, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_RuntimeFailureMapsToRuntimeError()
    {
        using var root = new TemporaryDirectory();
        var engine = new RecordingDockerEngine
        {
            Result = new DockerRunResult(
                1,
                string.Empty,
                "Traceback (most recent call last):\nValueError: bad query",
                90,
                false,
                false,
                false,
                false,
                false,
                null)
        };
        var runner = CreateRunner(engine, CreateSettings(root.Path));

        var outcome = await runner.ExecuteAsync(
            CreateRequest(),
            TestContext.Current.CancellationToken);

        Assert.Equal(ExecutionStatus.RuntimeError, outcome.Status);
    }

    private static SqlDockerRunner CreateRunner(
        IDockerEngine engine,
        ExecutionOptions settings)
    {
        return new SqlDockerRunner(
            engine,
            Options.Create(settings),
            NullLogger<SqlDockerRunner>.Instance);
    }

    private static ExecutionOptions CreateSettings(string workspaceRoot)
    {
        return new ExecutionOptions
        {
            Enabled = true,
            WorkspaceRoot = workspaceRoot
        };
    }

    private static ExecutionRequest CreateRequest()
    {
        return new ExecutionRequest(
            Guid.NewGuid(),
            "order-revenue",
            "sql",
            [new ExecutionFile("query.sql", "SELECT 1;")],
            "# harness",
            [
                new ExecutionTest(
                    "public-1",
                    "public",
                    """{"schema":"CREATE TABLE t (id INTEGER);","expected":[[1]]}""")
            ]);
    }
}
