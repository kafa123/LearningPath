namespace Challenge;

public static class Solution
{
    /// <summary>
    /// Returns the sum of numbers from <paramref name="start"/> through
    /// <paramref name="end"/>, inclusive.
    /// </summary>
    public static int RangeSum(int[] numbers, int start, int end)
    {
        var total = 0;

        for (var index = start; index < end; index++)
        {
            total += numbers[index];
        }

        return total;
    }
}
