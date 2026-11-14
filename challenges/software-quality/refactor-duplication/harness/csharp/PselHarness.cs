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
                var cases = test.GetProperty("data").GetProperty("cases").EnumerateArray().ToList();
                passed = cases.Count > 0;

                foreach (var testCase in cases)
                {
                    var call = testCase.GetProperty("call").GetString();
                    var tier = testCase.GetProperty("tier").GetString() ?? string.Empty;
                    var expected = testCase.GetProperty("expected");

                    if (call == "ApplyDiscount")
                    {
                        var price = testCase.GetProperty("price").GetDecimal();
                        var actual = Solution.ApplyDiscount(price, tier);

                        if (actual != expected.GetDecimal())
                        {
                            passed = false;
                            break;
                        }
                    }
                    else if (call == "ApplyDiscountToAll")
                    {
                        var prices = testCase.GetProperty("prices")
                            .EnumerateArray()
                            .Select(value => value.GetDecimal())
                            .ToArray();
                        var expectedPrices = expected
                            .EnumerateArray()
                            .Select(value => value.GetDecimal())
                            .ToArray();

                        var actual = Solution.ApplyDiscountToAll(prices, tier);

                        if (!actual.SequenceEqual(expectedPrices))
                        {
                            passed = false;
                            break;
                        }
                    }
                    else
                    {
                        passed = false;
                        break;
                    }
                }
            }
            catch (Exception)
            {
                passed = false;
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
