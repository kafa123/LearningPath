using Psel.Modules.Execution;
using Psel.Modules.Execution.Docker;

namespace Psel.Api.Tests;

public sealed class DockerArgumentTests
{
    [Fact]
    public void BuildRunArguments_AppliesServerControlledIsolationFlags()
    {
        var workspace = Path.Combine(Path.GetTempPath(), "psel-workspace");
        var request = new DockerRunRequest(
            "mcr.microsoft.com/dotnet/sdk:10.0",
            "psel-exec-test",
            workspace,
            "dotnet build Submission.csproj && dotnet out/Submission.dll",
            TimeSpan.FromSeconds(30),
            4096,
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1"
            });
        var settings = new ExecutionOptions
        {
            MemoryLimitMb = 256,
            CpuLimit = 0.5,
            PidsLimit = 64,
            ContainerUser = "1000:1000"
        };

        var arguments = ProcessDockerEngine.BuildRunArguments(request, settings);

        Assert.Equal("run", arguments[0]);
        Assert.Equal("none", ValueAfter(arguments, "--network"));
        Assert.Equal("256m", ValueAfter(arguments, "--memory"));
        Assert.Equal("256m", ValueAfter(arguments, "--memory-swap"));
        Assert.Equal("0.5", ValueAfter(arguments, "--cpus"));
        Assert.Equal("64", ValueAfter(arguments, "--pids-limit"));
        Assert.Contains("--read-only", arguments);
        Assert.StartsWith("/tmp:", ValueAfter(arguments, "--tmpfs"), StringComparison.Ordinal);
        Assert.Equal("ALL", ValueAfter(arguments, "--cap-drop"));
        Assert.Equal("no-new-privileges", ValueAfter(arguments, "--security-opt"));
        Assert.Equal("1000:1000", ValueAfter(arguments, "--user"));
        Assert.Equal($"{workspace}:/workspace:ro", ValueAfter(arguments, "--volume"));
        Assert.Equal("/workspace", ValueAfter(arguments, "--workdir"));
        Assert.Equal("DOTNET_CLI_TELEMETRY_OPTOUT=1", ValueAfter(arguments, "--env"));

        Assert.Equal(
            [
                "mcr.microsoft.com/dotnet/sdk:10.0",
                "/bin/sh",
                "-c",
                "dotnet build Submission.csproj && dotnet out/Submission.dll"
            ],
            arguments.Skip(arguments.Count - 4));

        Assert.DoesNotContain("--privileged", arguments);
        Assert.DoesNotContain("host", arguments);
        Assert.DoesNotContain("/var/run/docker.sock", arguments);
    }

    private static string ValueAfter(IReadOnlyList<string> arguments, string flag)
    {
        for (var index = 0; index < arguments.Count - 1; index++)
        {
            if (string.Equals(arguments[index], flag, StringComparison.Ordinal))
            {
                return arguments[index + 1];
            }
        }

        throw new InvalidOperationException($"Flag '{flag}' was not present.");
    }
}
