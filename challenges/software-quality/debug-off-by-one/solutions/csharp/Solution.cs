namespace Challenge;

public static class Solution
{
    public static int RangeSum(int[] numbers, int start, int end)
    {
        var total = 0;

        for (var index = start; index <= end; index++)
        {
            total += numbers[index];
        }

        return total;
    }
}
