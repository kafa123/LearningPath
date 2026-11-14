using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Psel.Modules.Execution;
using Psel.Modules.Execution.Docker;
using Psel.Modules.Execution.Runners;

namespace Psel.Api.Tests;

public sealed class CSharpDockerRunnerTests
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
                "0,1\n",
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
                "csharp",
                [
                    new ExecutionFile("Solution.cs", "// submitted code"),
                    new ExecutionFile("Nested/Extra.cs", "// extra code")
                ],
                "// SERVER_HARNESS",
                [
                    new ExecutionTest(
                        "public-1",
                        "public",
                        """{"input":{"numbers":[2,7,11,15],"target":9},"expected":[0,1]}""")
                ]),
            cancellationToken);

        Assert.Equal(ExecutionStatus.Succeeded, outcome.Status);
        Assert.Equal(0, outcome.ExitCode);
        Assert.Equal("0,1\n", outcome.StandardOutput);

        var request = Assert.IsType<DockerRunRequest>(engine.Request);
        Assert.Equal(settings.CSharpImage, request.Image);
        Assert.Equal($"psel-exec-{submissionId:N}", request.ContainerName);
        Assert.Contains("dotnet build Submission.csproj", request.Command, StringComparison.Ordinal);
        Assert.Contains("dotnet /tmp/out/Submission.dll", request.Command, StringComparison.Ordinal);
        Assert.Contains("BaseIntermediateOutputPath=/tmp/obj/", request.Command, StringComparison.Ordinal);
        Assert.Equal("1", request.EnvironmentVariables["DOTNET_CLI_TELEMETRY_OPTOUT"]);

        Assert.Contains("<OutputType>Exe</OutputType>", engine.Files["Submission.csproj"]);
        Assert.Equal("// SERVER_HARNESS", engine.Files["PselHarness.cs"]);
        Assert.Equal("// submitted code", engine.Files["Solution.cs"]);
        Assert.Equal("// extra code", engine.Files["Nested/Extra.cs"]);
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

        var outcome = await runner.ExecuteAsync(CreateRequest(), TestContext.Current.CancellationToken);

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
    [InlineData("PselHarness.cs")]
    [InlineData("PselTests.json")]
    [InlineData("Submission.csproj")]
    public async Task ExecuteAsync_ReservedServerFile_RejectsBeforeDocker(string path)
    {
        using var root = new TemporaryDirectory();
        var engine = new RecordingDockerEngine();
        var runner = CreateRunner(engine, CreateSettings(root.Path));

        var outcome = await runner.ExecuteAsync(
            CreateRequest() with { Files = [new ExecutionFile(path, "// code")] },
            TestContext.Current.CancellationToken);

        Assert.Equal(ExecutionStatus.InternalError, outcome.Status);
        Assert.Null(engine.Request);
    }

    [Theory]
    [InlineData("../escape.cs")]
    [InlineData("/absolute.cs")]
    [InlineData("nested/../../escape.cs")]
    public async Task ExecuteAsync_UnsafePath_RejectsBeforeDocker(string path)
    {
        using var root = new TemporaryDirectory();
        var engine = new RecordingDockerEngine();
        var runner = CreateRunner(engine, CreateSettings(root.Path));

        var outcome = await runner.ExecuteAsync(
            CreateRequest() with { Files = [new ExecutionFile(path, "// code")] },
            TestContext.Current.CancellationToken);

        Assert.Equal(ExecutionStatus.InternalError, outcome.Status);
        Assert.Null(engine.Request);
    }

    [Fact]
    public async Task ExecuteAsync_NonCSharpFile_RejectsBeforeDocker()
    {
        using var root = new TemporaryDirectory();
        var engine = new RecordingDockerEngine();
        var runner = CreateRunner(engine, CreateSettings(root.Path));

        var outcome = await runner.ExecuteAsync(
            CreateRequest() with { Files = [new ExecutionFile("solution.py", "print(1)")] },
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

        var outcome = await runner.ExecuteAsync(CreateRequest(), TestContext.Current.CancellationToken);

        Assert.Equal(ExecutionStatus.TimeLimitExceeded, outcome.Status);
    }

    [Fact]
    public async Task ExecuteAsync_CompilationDiagnosticsMapToCompilationError()
    {
        using var root = new TemporaryDirectory();
        var engine = new RecordingDockerEngine
        {
            Result = new DockerRunResult(
                1,
                string.Empty,
                "Solution.cs(1,1): error CS1002: ; expected",
                120,
                false,
                false,
                false,
                false,
                false,
                null)
        };
        var runner = CreateRunner(engine, CreateSettings(root.Path));

        var outcome = await runner.ExecuteAsync(CreateRequest(), TestContext.Current.CancellationToken);

        Assert.Equal(ExecutionStatus.CompilationError, outcome.Status);
        Assert.Contains("error CS1002", outcome.StandardError, StringComparison.Ordinal);
    }

    private static CSharpDockerRunner CreateRunner(
        IDockerEngine engine,
        ExecutionOptions settings)
    {
        return new CSharpDockerRunner(
            engine,
            Options.Create(settings),
            NullLogger<CSharpDockerRunner>.Instance);
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
            "csharp",
            [new ExecutionFile("Solution.cs", "// code")],
            "// harness",
            [
                new ExecutionTest(
                    "public-1",
                    "public",
                    """{"input":{"numbers":[2,7,11,15],"target":9},"expected":[0,1]}""")
            ]);
    }
}
