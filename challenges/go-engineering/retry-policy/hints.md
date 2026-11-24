# Hints

- Clamp `MaxAttempts` once at the top of `Run`; every later branch can then trust it.
- Use `var retryable RetryableError` and `errors.As(err, &retryable)` inside `IsRetryable`.
  `errors.As` walks `fmt.Errorf` and `Unwrap` chains.
- Build the `Report` incrementally so a non-retryable failure and an exhausted failure both
  return what was actually attempted.
- Compute the delay from the attempt number, not from the previous delay. That keeps the
  cap simple: start at `BaseDelay`, double `attempt-1` times, and clamp at `MaxDelay`.
- Check `BaseDelay <= 0` before computing or recording anything. Do not call `Sleep(0)`.
- Initialize the report with `Delays: []time.Duration{}` so the JSON comparison sees `[]`
  instead of `null`.
- An exhausted failure still needs `%w`; the harness checks it with `errors.Is`.
