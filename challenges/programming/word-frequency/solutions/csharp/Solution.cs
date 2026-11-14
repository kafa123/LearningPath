using System.Collections.Generic;
using System.Linq;

namespace Challenge;

public static class Solution
{
    public static string[] TopWords(string text, int count)
    {
        var counts = new Dictionary<string, int>();

        foreach (var token in text.Split(
            (char[]?)null,
            System.StringSplitOptions.RemoveEmptyEntries))
        {
            var word = token.Trim().TrimPunctuation().ToLowerInvariant();

            if (word.Length == 0)
            {
                continue;
            }

            counts[word] = counts.GetValueOrDefault(word) + 1;
        }

        return counts
            .OrderByDescending(entry => entry.Value)
            .ThenBy(entry => entry.Key, System.StringComparer.Ordinal)
            .Take(count)
            .Select(entry => entry.Key)
            .ToArray();
    }

    private static string TrimPunctuation(this string value)
    {
        var start = 0;
        var end = value.Length - 1;

        while (start <= end && char.IsPunctuation(value[start]))
        {
            start++;
        }

        while (end >= start && char.IsPunctuation(value[end]))
        {
            end--;
        }

        return start > end ? string.Empty : value[start..(end + 1)];
    }
}
