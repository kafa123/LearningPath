import string


def top_words(text: str, count: int) -> list[str]:
    counts: dict[str, int] = {}

    for token in text.split():
        word = token.strip(string.punctuation).lower()
        if not word:
            continue
        counts[word] = counts.get(word, 0) + 1

    ordered = sorted(counts.items(), key=lambda item: (-item[1], item[0]))
    return [word for word, _ in ordered[:count]]
