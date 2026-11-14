using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
                var input = test.GetProperty("data").GetProperty("input");
                var expected = test.GetProperty("data").GetProperty("expected");

                var numbers = input.GetProperty("numbers")
                    .EnumerateArray()
                    .Select(value => value.GetInt32())
                    .ToArray();
                var start = input.GetProperty("start").GetInt32();
                var end = input.GetProperty("end").GetInt32();

                passed = Solution.RangeSum(numbers, start, end) == expected.GetInt32();
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

    private sealed class Result
    {
        public string Id { get; set; } = string.Empty;

        public bool Passed { get; set; }

        public bool Errored { get; set; }
    }
}
