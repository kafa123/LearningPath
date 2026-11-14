# Word Frequency

Given a block of text and a number `count`, return the `count` most frequent words.

Rules:

- Words are separated by whitespace.
- Normalize each word to lowercase.
- Remove punctuation from the beginning and end of each word.
- Ignore empty results after trimming punctuation.
- Sort by descending frequency, then alphabetically for equal counts.
- Return at most `count` words.

## Function contract

```text
TopWords(text: string, count: int) -> string[]
```

```text
top_words(text: str, count: int) -> list[str]
```

## Example

```text
text  = "the cat and the dog"
count = 2
result = ["the", "and"]
```

`the` appears twice. The other words appear once, so alphabetical order decides the second slot.

## Constraints

- `0 <= text.length <= 1,000,000`
- `1 <= count <= 100`
- Punctuation that matters is at the edges of words; internal apostrophes may remain.
