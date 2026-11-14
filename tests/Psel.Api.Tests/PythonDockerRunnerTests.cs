using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Psel.Modules.Execution;
using Psel.Modules.Execution.Docker;
using Psel.Modules.Execution.Runners;

namespace Psel.Api.Tests;

public sealed class PythonDockerRunnerTests
{
    [Fact]
    public async Task ExecuteAsync_WritesServerWorkspaceAndMapsSuccess()
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
                25,
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
                "two-sum",
                "python",
                [
                    new ExecutionFile("solution.py", "def two_sum(numbers, target): ..."),
                    new ExecutionFile("Nested/helpers.py", "VALUE = 1")
                ],
                "# SERVER_HARNESS",
                [
                    new ExecutionTest(
                        "public-1",
                        "public",
                        """{"input":{"numbers":[2,7,11,15],"target":9},"expected":[0,1]}""")
                ]),
            cancellationToken);

        Assert.Equal(ExecutionStatus.Succeeded, outcome.Status);
        Assert.Equal(0, outcome.ExitCode);

        var request = Assert.IsType<DockerRunRequest>(engine.Request);
        Assert.Equal(settings.PythonImage, request.Image);
        Assert.Equal($"psel-exec-{submissionId:N}", request.ContainerName);
        Assert.Contains("python3 -B /workspace/PselHarness.py", request.Command, StringComparison.Ordinal);
        Assert.Equal("1", request.EnvironmentVariables["PYTHONDONTWRITEBYTECODE"]);
        Assert.Equal("/tmp/pycache", request.EnvironmentVariables["PYTHONPYCACHEPREFIX"]);
        Assert.Equal("/tmp", request.EnvironmentVariables["HOME"]);

        Assert.Equal("# SERVER_HARNESS", engine.Files["PselHarness.py"]);
        Assert.Equal("def two_sum(numbers, target): ...", engine.Files["solution.py"]);
        Assert.Equal("VALUE = 1", engine.Files["Nested/helpers.py"]);
        Assert.Contains("\"id\":\"public-1\"", engine.Files["PselTests.json"], StringComparison.Ordinal);
        Assert.Contains("\"visibility\":\"public\"", engine.Files["PselTests.json"], StringComparison.Ordinal);
        Assert.Contains("\"numbers\":[2,7,11,15]", engine.Files["PselTests.json"], StringComparison.Ordinal);
        Assert.True(engine.WorkspaceExistedDuringRun);

        Assert.False(Directory.Exists(request.WorkspacePath));
    }

    [Fact]
    public async Task ExecuteAsync_WhenDockerUnavailable_ReturnsExecutionUnavailable()
    {
        using var root = new TemporaryDirectory();
        var engine = new RecordingDockerEngine { Available = false };
        var runner = CreateRunner(engine, CreateSettings(root.Path));

        var outcome = await runner.ExecuteAsync(
            CreateRequest(),
            TestContext.Current.CancellationToken);

        Assert.Equal(ExecutionStatus.ExecutionUnavailable, outcome.Status);
        Assert.Null(engine.Request);
        Assert.Contains("Docker is not available", outcome.FailureMessage, StringComparison.Ordinal);
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

    [Theory]
    [InlineData("PselHarness.py")]
    [InlineData("PselTests.json")]
    public async Task ExecuteAsync_ReservedServerFile_RejectsBeforeDocker(string path)
    {
        using var root = new TemporaryDirectory();
        var engine = new RecordingDockerEngine();
        var runner = CreateRunner(engine, CreateSettings(root.Path));

        var outcome = await runner.ExecuteAsync(
            CreateRequest() with { Files = [new ExecutionFile(path, "print(1)")] },
            TestContext.Current.CancellationToken);

        Assert.Equal(ExecutionStatus.InternalError, outcome.Status);
        Assert.Null(engine.Request);
    }

    [Theory]
    [InlineData("../escape.py")]
    [InlineData("/absolute.py")]
    [InlineData("nested/../../escape.py")]
    public async Task ExecuteAsync_UnsafePath_RejectsBeforeDocker(string path)
    {
        using var root = new TemporaryDirectory();
        var engine = new RecordingDockerEngine();
        var runner = CreateRunner(engine, CreateSettings(root.Path));

        var outcome = await runner.ExecuteAsync(
            CreateRequest() with { Files = [new ExecutionFile(path, "print(1)")] },
            TestContext.Current.CancellationToken);

        Assert.Equal(ExecutionStatus.InternalError, outcome.Status);
        Assert.Null(engine.Request);
    }

    [Fact]
    public async Task ExecuteAsync_NonPythonFile_RejectsBeforeDocker()
    {
        using var root = new TemporaryDirectory();
        var engine = new RecordingDockerEngine();
        var runner = CreateRunner(engine, CreateSettings(root.Path));

        var outcome = await runner.ExecuteAsync(
            CreateRequest() with { Files = [new ExecutionFile("Solution.cs", "// code")] },
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
            CreateRequest() with { LanguageId = "csharp" },
            TestContext.Current.CancellationToken);

        Assert.Equal(ExecutionStatus.InternalError, outcome.Status);
        Assert.Null(engine.Request);
    }

    [Fact]
    public async Task ExecuteAsync_TimeoutMapsToTimeLimitExceeded()
    {
        using var root = new TemporaryDirectory();
        var engine = new RecordingDockerEngine
        {
            Result = new DockerRunResult(
                null,
                string.Empty,
                string.Empty,
                60_000,
                true,
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

        Assert.Equal(ExecutionStatus.TimeLimitExceeded, outcome.Status);
    }

    [Fact]
    public async Task ExecuteAsync_SyntaxErrorMapsToCompilationError()
    {
        using var root = new TemporaryDirectory();
        var engine = new RecordingDockerEngine
        {
            Result = new DockerRunResult(
                1,
                string.Empty,
                "  File \"/workspace/solution.py\", line 1\n    def broken(\n             ^\nSyntaxError: '(' was never closed",
                120,
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
        Assert.Contains("SyntaxError", outcome.StandardError, StringComparison.Ordinal);
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
                "Traceback (most recent call last):\nValueError: no solution",
                120,
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

    private static PythonDockerRunner CreateRunner(
        IDockerEngine engine,
        ExecutionOptions settings)
    {
        return new PythonDockerRunner(
            engine,
            Options.Create(settings),
            NullLogger<PythonDockerRunner>.Instance);
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
            "two-sum",
            "python",
            [new ExecutionFile("solution.py", "def two_sum(numbers, target): ...")],
            "# harness",
            [
                new ExecutionTest(
                    "public-1",
                    "public",
                    """{"input":{"numbers":[2,7,11,15],"target":9},"expected":[0,1]}""")
            ]);
    }
}
