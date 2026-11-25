using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Psel.Modules.Execution;
using Psel.Modules.Execution.Docker;
using Psel.Modules.Execution.Runners;

namespace Psel.Api.Tests;

public sealed class GoDockerRunnerTests
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
                "inventory-ledger",
                "go",
                [
                    new ExecutionFile("solution.go", "package main\n\nfunc Normalize() {}"),
                    new ExecutionFile("Nested/helpers.go", "package main\n\nvar Value = 1")
                ],
                "// SERVER_HARNESS",
                [
                    new ExecutionTest(
                        "public-1",
                        "public",
                        """{"input":{"items":[]},"expected":[]}""")
                ]),
            cancellationToken);

        Assert.Equal(ExecutionStatus.Succeeded, outcome.Status);
        Assert.Equal(0, outcome.ExitCode);

        var request = Assert.IsType<DockerRunRequest>(engine.Request);
        Assert.Equal(settings.GoImage, request.Image);
        Assert.Equal($"psel-exec-{submissionId:N}", request.ContainerName);
        Assert.Contains("go build -trimpath -o /tmp/psel-submission .", request.Command, StringComparison.Ordinal);
        Assert.Contains("/tmp/psel-submission", request.Command, StringComparison.Ordinal);
        Assert.Equal("/tmp", request.EnvironmentVariables["HOME"]);
        Assert.Equal("/tmp/gocache", request.EnvironmentVariables["GOCACHE"]);
        Assert.Equal("/tmp/go", request.EnvironmentVariables["GOPATH"]);
        Assert.Equal("on", request.EnvironmentVariables["GO111MODULE"]);
        Assert.Equal("local", request.EnvironmentVariables["GOTOOLCHAIN"]);
        Assert.Equal("off", request.EnvironmentVariables["GOPROXY"]);
        Assert.Equal("0", request.EnvironmentVariables["CGO_ENABLED"]);

        Assert.Equal("// SERVER_HARNESS", engine.Files["PselHarness.go"]);
        Assert.Equal("package main\n\nfunc Normalize() {}", engine.Files["solution.go"]);
        Assert.Equal("package main\n\nvar Value = 1", engine.Files["Nested/helpers.go"]);
        Assert.Contains("module psel/submission", engine.Files["go.mod"], StringComparison.Ordinal);
        Assert.Contains("\"id\":\"public-1\"", engine.Files["PselTests.json"], StringComparison.Ordinal);
        Assert.Contains("\"visibility\":\"public\"", engine.Files["PselTests.json"], StringComparison.Ordinal);
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
    [InlineData("PselHarness.go")]
    [InlineData("go.mod")]
    [InlineData("PselTests.json")]
    public async Task ExecuteAsync_ReservedServerFile_RejectsBeforeDocker(string path)
    {
        using var root = new TemporaryDirectory();
        var engine = new RecordingDockerEngine();
        var runner = CreateRunner(engine, CreateSettings(root.Path));

        var outcome = await runner.ExecuteAsync(
            CreateRequest() with { Files = [new ExecutionFile(path, "package main")] },
            TestContext.Current.CancellationToken);

        Assert.Equal(ExecutionStatus.InternalError, outcome.Status);
        Assert.Null(engine.Request);
    }

    [Theory]
    [InlineData("../escape.go")]
    [InlineData("/absolute.go")]
    [InlineData("nested/../../escape.go")]
    public async Task ExecuteAsync_UnsafePath_RejectsBeforeDocker(string path)
    {
        using var root = new TemporaryDirectory();
        var engine = new RecordingDockerEngine();
        var runner = CreateRunner(engine, CreateSettings(root.Path));

        var outcome = await runner.ExecuteAsync(
            CreateRequest() with { Files = [new ExecutionFile(path, "package main")] },
            TestContext.Current.CancellationToken);

        Assert.Equal(ExecutionStatus.InternalError, outcome.Status);
        Assert.Null(engine.Request);
    }

    [Fact]
    public async Task ExecuteAsync_NonGoFile_RejectsBeforeDocker()
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
            CreateRequest() with { LanguageId = "python" },
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
    public async Task ExecuteAsync_CompileErrorMapsToCompilationError()
    {
        using var root = new TemporaryDirectory();
        var engine = new RecordingDockerEngine
        {
            Result = new DockerRunResult(
                1,
                string.Empty,
                "# psel/submission\n./solution.go:5:2: undefined: missingHelper",
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
        Assert.Contains("undefined: missingHelper", outcome.StandardError, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_RuntimeFailureMapsToRuntimeError()
    {
        using var root = new TemporaryDirectory();
        var engine = new RecordingDockerEngine
        {
            Result = new DockerRunResult(
                2,
                string.Empty,
                "panic: runtime error: index out of range [3] with length 3",
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

    private static GoDockerRunner CreateRunner(
        IDockerEngine engine,
        ExecutionOptions settings)
    {
        return new GoDockerRunner(
            engine,
            Options.Create(settings),
            NullLogger<GoDockerRunner>.Instance);
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
            "inventory-ledger",
            "go",
            [new ExecutionFile("solution.go", "package main\n\nfunc Normalize() {}")],
            "// harness",
            [
                new ExecutionTest(
                    "public-1",
                    "public",
                    """{"input":{"items":[]},"expected":[]}""")
            ]);
    }
}
