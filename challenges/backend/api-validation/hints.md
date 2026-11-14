# Hints

1. Collect errors in a `Dictionary<string, string[]>` before deciding the response.
2. `string.IsNullOrWhiteSpace` catches missing and whitespace-only values.
3. Trim the display name before checking its length so padding cannot bypass the rule.
4. `Results.ValidationProblem(errors)` returns a standard ProblemDetails response with status 400.
5. Check every field before returning so the caller can fix all problems at once.
