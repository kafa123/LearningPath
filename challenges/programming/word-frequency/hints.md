# Hints

1. Split the text on whitespace first, then normalize each token.
2. `char.IsPunctuation` in C# and `string.punctuation` in Python can help trim edges.
3. Count with a dictionary keyed by the normalized word.
4. A custom sort is easier to reason about than two separate sorts: compare count descending, then word ascending.
5. Returning fewer than `count` words is valid when there are fewer distinct words.
