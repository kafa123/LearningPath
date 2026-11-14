def is_balanced(text: str) -> bool:
    pairs = {")": "(", "]": "[", "}": "{"}
    openers: list[str] = []

    for character in text:
        if character in "([{":
            openers.append(character)
        elif character in pairs:
            if not openers or openers.pop() != pairs[character]:
                return False

    return not openers
