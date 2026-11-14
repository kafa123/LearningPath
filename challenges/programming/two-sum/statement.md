# Two Sum

Given an array of integers `numbers` and an integer `target`, return the indices of the two numbers that add up to `target`.

You may assume that each input has **exactly one solution**, and you may not use the same element twice. Return the indices in ascending order.

## Function contract

```text
TwoSum(numbers: int[], target: int) -> int[2]
```

```text
two_sum(numbers: list[int], target: int) -> list[int]
```

## Approach

A direct nested loop checks every pair, which is O(n²). A hash map lets you remember the values you have already seen:

1. Walk through the array once.
2. For each value, compute the complement `target - value`.
3. If the complement is already in the map, return both indices.
4. Otherwise store the current value with its index.

This runs in O(n) average time and uses O(n) extra space.

## Constraints

- `2 <= numbers.length <= 10,000`
- Values fit in a signed 32-bit integer.
- Each input has exactly one answer.
