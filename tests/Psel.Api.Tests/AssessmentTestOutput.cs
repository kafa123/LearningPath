using System.Text.Json;
using Psel.Modules.Execution;

namespace Psel.Api.Tests;

/// <summary>
/// Builds the machine-readable result block emitted by trusted challenge
/// harnesses. Tests use it to drive deterministic assessment scenarios.
/// </summary>
internal static class AssessmentTestOutput
{
    public const string BeginMarker = "__PSEL_RESULTS_BEGIN__";
    public const string EndMarker = "__PSEL_RESULTS_END__";

    public static string Build(
        ExecutionRequest request,
        Func<ExecutionTest, (bool Passed, bool Errored)>? observe = null)
    {
        var results = request.Tests!
            .Select(test =>
            {
                var (passed, errored) = observe?.Invoke(test) ?? (true, false);

                return new
                {
                    id = test.Id,
                    passed,
                    errored
                };
            })
            .ToArray();

        return string.Concat(
            "harness progress\n",
            BeginMarker,
            "\n",
            JsonSerializer.Serialize(results),
            "\n",
            EndMarker,
            "\n");
    }

    public static string BuildFromObservations(
        params (string Id, bool Passed, bool Errored)[] observations)
    {
        return string.Concat(
            BeginMarker,
            "\n",
            JsonSerializer.Serialize(observations.Select(observation => new
            {
                id = observation.Id,
                passed = observation.Passed,
                errored = observation.Errored
            })),
            "\n",
            EndMarker,
            "\n");
    }

    public static ExecutionOutcome Success(string standardOutput)
    {
        return new ExecutionOutcome(
            ExecutionStatus.Succeeded,
            0,
            12,
            standardOutput,
            string.Empty,
            null,
            false);
    }
}
