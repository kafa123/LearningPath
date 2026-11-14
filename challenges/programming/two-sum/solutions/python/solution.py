def two_sum(numbers: list[int], target: int) -> list[int]:
    seen: dict[int, int] = {}

    for index, value in enumerate(numbers):
        complement = target - value
        if complement in seen:
            return [seen[complement], index]
        seen[value] = index

    raise ValueError("The input does not contain a solution.")
