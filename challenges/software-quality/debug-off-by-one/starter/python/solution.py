def range_sum(numbers: list[int], start: int, end: int) -> int:
    """Return the sum from start through end, inclusive."""
    total = 0
    for index in range(start, end):
        total += numbers[index]
    return total
