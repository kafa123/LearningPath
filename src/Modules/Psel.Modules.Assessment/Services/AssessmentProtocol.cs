using System.Text.Json;

namespace Psel.Modules.Assessment.Services;

internal sealed record ObservedTest(string? Id, bool Passed, bool Errored);

/// <summary>
/// Reads the machine-readable test block emitted by trusted challenge harnesses.
/// The block is stripped from output before it is persisted for the client.
/// </summary>
internal static class AssessmentProtocol
{
    internal const string BeginMarker = "__PSEL_RESULTS_BEGIN__";
    internal const string EndMarker = "__PSEL_RESULTS_END__";

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    public static IReadOnlyList<ObservedTest>? TryRead(string standardOutput)
    {
        if (!TryFindBlock(standardOutput, out var start, out var end))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<List<ObservedTest>>(
                standardOutput[(start + BeginMarker.Length)..(end - EndMarker.Length)],
                JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static string StripResults(string standardOutput)
    {
        if (!TryFindBlock(standardOutput, out var start, out var end))
        {
            return standardOutput;
        }

        return string.Concat(standardOutput[..start], standardOutput[end..]).TrimEnd();
    }

    private static bool TryFindBlock(string output, out int start, out int end)
    {
        start = output.LastIndexOf(BeginMarker, StringComparison.Ordinal);
        end = start < 0
            ? -1
            : output.IndexOf(EndMarker, start + BeginMarker.Length, StringComparison.Ordinal);

        if (start < 0 || end < 0)
        {
            start = -1;
            end = -1;
            return false;
        }

        end += EndMarker.Length;
        return true;
    }
}
