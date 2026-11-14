namespace Challenge;

public static class Solution
{
    public static int[] TwoSum(int[] numbers, int target)
    {
        var seen = new Dictionary<int, int>();

        for (var index = 0; index < numbers.Length; index++)
        {
            var complement = target - numbers[index];

            if (seen.TryGetValue(complement, out var complementIndex))
            {
                return [complementIndex, index];
            }

            seen[numbers[index]] = index;
        }

        throw new InvalidOperationException("The input does not contain a solution.");
    }
}
