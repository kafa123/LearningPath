# Hints

- Normalize once: `errorCode?.Trim().ToLowerInvariant()` in C#, or `(error_code or "").strip().lower()` in Python.
- A `switch` expression or a lookup table keeps the mapping readable.
- The fallback branch should still return a complete response, not `null`.
- Add a test case with a very loud `internalDetail` and confirm it is absent from `detail`.
