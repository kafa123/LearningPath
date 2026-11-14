using System.Text.Json;

namespace Psel.Modules.Execution.Runners;

/// <summary>
/// Writes the server-selected test manifest that trusted harnesses read at
/// execution time. Test data stays server-side; only the opaque identifier and
/// pass/fail evidence are returned by the harness.
/// </summary>
internal static class TestManifestWriter
{
    public const string FileName = "PselTests.json";

    public static void Write(
        string workspace,
        IReadOnlyList<ExecutionTest> tests)
    {
        using var stream = File.Create(Path.Combine(workspace, FileName));
        using var writer = new Utf8JsonWriter(stream);

        writer.WriteStartObject();
        writer.WriteNumber("schemaVersion", 1);
        writer.WritePropertyName("tests");
        writer.WriteStartArray();

        foreach (var test in tests)
        {
            writer.WriteStartObject();
            writer.WriteString("id", test.Id);
            writer.WriteString("visibility", test.Visibility);
            writer.WritePropertyName("data");

            using var data = JsonDocument.Parse(test.DataJson);
            data.RootElement.WriteTo(writer);

            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
    }
}
