# Debug an Off-by-One Sum

`RangeSum` should return the sum of every element from index `start` through index `end`, **inclusive**. The current implementation passes some cases and fails others.

```csharp
public static int RangeSum(int[] numbers, int start, int end)
{
    var total = 0;
    for (var i = start; i < end; i++)
    {
        total += numbers[i];
    }
    return total;
}
```

## Your task

1. Read the contract and the loop condition carefully.
2. Find a small input that fails.
3. Fix the boundary so both endpoints are included.
4. Keep the function signature unchanged.

## Function contract

```text
RangeSum(numbers: int[], start: int, end: int) -> int
```

```text
range_sum(numbers: list[int], start: int, end: int) -> int
```

## Constraints

- `0 <= start <= end < numbers.length`
- Values may be negative.
- Do not special-case individual test inputs.
