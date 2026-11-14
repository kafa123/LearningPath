def range_sum(numbers: list[int], start: int, end: int) -> int:
    total = 0
    for index in range(start, end + 1):
        total += numbers[index]
    return total
