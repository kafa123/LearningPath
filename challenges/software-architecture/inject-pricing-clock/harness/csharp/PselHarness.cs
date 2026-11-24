using System;
using System.Collections.Generic;
using System.Globalization;
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
                    var basePrice = testCase.GetProperty("basePriceCents").GetInt32();
                    var coupon = ReadNullableString(testCase, "coupon");
                    var nowText = testCase.GetProperty("now").GetString() ?? string.Empty;
                    var now = DateTimeOffset.Parse(
                        nowText,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
                    var expected = testCase.GetProperty("expected").GetInt32();
                    var actual = Solution.PriceCents(basePrice, coupon, now);

                    if (actual != expected)
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

    private sealed class Result
    {
        public string Id { get; set; } = string.Empty;

        public bool Passed { get; set; }

        public bool Errored { get; set; }
    }
}
