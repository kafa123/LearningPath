# Balanced Brackets

Write a function that decides whether the brackets in a string are balanced.

A string is balanced when:

- Every opening bracket `(`, `[`, or `{` has a matching closing bracket.
- Brackets close in the reverse order they were opened.
- No closing bracket appears without a matching opener.

Characters that are not brackets are ignored.

## Function contract

```text
IsBalanced(text: string) -> bool
```

```text
is_balanced(text: str) -> bool
```

## Approach

Scan the string from left to right and keep a stack of opening brackets.

- On an opening bracket, push it.
- On a closing bracket, the stack must be non-empty and its top must be the matching opener. Pop it.
- At the end, the stack must be empty.

This is O(n) time and O(n) space in the worst case.

## Constraints

- `0 <= text.length <= 100,000`
- Only `()[]{}` are treated as brackets.
