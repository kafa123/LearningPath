using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Challenge;

/// <summary>
/// Server-side execution harness for this challenge. It reads the server-written
/// test manifest and emits only opaque test identifiers and pass/fail evidence.
/// It is never sent to the browser.
/// </summary>
public static class Program
{
    private const string TestsPath = "/workspace/PselTests.json";

    public static void Main()
    {
        var results = new List<Result>();
        using var document = JsonDocument.Parse(File.ReadAllText(TestsPath));

        foreach (var test in document.RootElement.GetProperty("tests").EnumerateArray())
        {
            var id = test.GetProperty("id").GetString() ?? string.Empty;
            var passed = false;
            var errored = false;

            try
            {
                var cases = test.GetProperty("data").GetProperty("cases");
                passed = cases.GetArrayLength() > 0;

                foreach (var testCase in cases.EnumerateArray())
                {
                    var orderElement = testCase.GetProperty("order");
                    var order = new Order(
                        orderElement.GetProperty("id").GetInt32(),
                        orderElement.GetProperty("totalCents").GetInt32(),
                        ReadNullableString(orderElement, "recipient"));

                    var expectedCode = testCase.GetProperty("expectedCode").GetString();
                    var expectedMessages = testCase.GetProperty("sentMessages");
                    var recorder = new RecordingNotifier();

                    var actualCode = Checkout.Complete(order, recorder);

                    if (!string.Equals(actualCode, expectedCode, StringComparison.Ordinal)
                        || expectedMessages.GetArrayLength() != recorder.Sends.Count)
                    {
                        passed = false;
                        break;
                    }

                    var messageIndex = 0;
                    var messagesMatch = true;

                    foreach (var expectedMessage in expectedMessages.EnumerateArray())
                    {
                        var actual = recorder.Sends[messageIndex];

                        if (!string.Equals(
                                actual.Recipient,
                                expectedMessage.GetProperty("recipient").GetString(),
                                StringComparison.Ordinal)
                            || !string.Equals(
                                actual.Message,
                                expectedMessage.GetProperty("message").GetString(),
                                StringComparison.Ordinal))
                        {
                            messagesMatch = false;
                            break;
                        }

                        messageIndex++;
                    }

                    if (!messagesMatch)
                    {
                        passed = false;
                        break;
                    }
                }
            }
            catch (Exception)
            {
                errored = true;
            }

            results.Add(new Result { Id = id, Passed = passed, Errored = errored });
        }

        Console.WriteLine("__PSEL_RESULTS_BEGIN__");
        Console.WriteLine(JsonSerializer.Serialize(results));
        Console.WriteLine("__PSEL_RESULTS_END__");
    }

    private static string? ReadNullableString(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var value)
            || value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        return value.GetString();
    }

    private sealed class RecordingNotifier : INotifier
    {
        public List<(string Recipient, string Message)> Sends { get; } = [];

        public void Send(string recipient, string message)
        {
            Sends.Add((recipient, message));
        }
    }

    private sealed class Result
    {
        public string Id { get; set; } = string.Empty;

        public bool Passed { get; set; }

        public bool Errored { get; set; }
    }
}
