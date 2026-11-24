# Retry with a deterministic sleeper

`Run` executes an operation with bounded retries. The operation can mark a failure as
retryable, and delays are measured and delivered through an injected `Sleeper` rather
than `time.Sleep`. The starter retries every error, never grows the delay, and never
wraps the final failure.

Implement the retry policy exactly as specified so it can be tested without a clock.

## Public API

```go
type Sleeper interface {
	Sleep(d time.Duration)
}

type Policy struct {
	MaxAttempts int
	BaseDelay   time.Duration
	MaxDelay    time.Duration
}

type Report struct {
	Attempts int
	Delays   []time.Duration
}

type RetryableError struct {
	Err error
}

func (e RetryableError) Error() string
func (e RetryableError) Unwrap() error

var ErrNotRetryable = errors.New("not retryable")

func IsRetryable(err error) bool

func Run(sleeper Sleeper, policy Policy, operation func(attempt int) error) (Report, error)
```

Implement everything in `solution.go`. Do not add a `main` function; the server-side
harness supplies the entry point.

## Rules

1. `MaxAttempts < 1` clamps to `1`. `operation` receives attempt numbers starting at `1`
   and is called exactly once per attempt.
2. If `operation` returns `nil`, return the report and `nil`.
3. Retry only when `IsRetryable(err)` is true. `IsRetryable` must report true when
   `errors.As(err, &retryable)` succeeds for a `RetryableError` value, including when
   that value is wrapped by `fmt.Errorf` or by `RetryableError` itself.
4. A non-retryable error stops immediately. Return the report built so far and the error
   unchanged, so `errors.Is` and `errors.As` still reach the original sentinel.
5. When attempts remain, the delay before attempt `n+1` is
   `BaseDelay * 2^(n-1)`, capped at `MaxDelay` when `MaxDelay > 0`. Append the delay to
   `Report.Delays` and pass the same value to `sleeper.Sleep`.
6. `BaseDelay <= 0` disables sleeping: call `sleeper.Sleep` zero times and keep
   `Report.Delays` empty. Retries still happen.
7. When the final attempt fails with a retryable error, return the full report and
   `fmt.Errorf("giving up after %d attempts: %w", attempts, lastErr)`.
8. `Report.Delays` is always a non-nil slice. It is `[]time.Duration{}` when nothing was
   slept, never `nil`.
9. Never call `time.Sleep`, `time.Now`, timers, or any other clock source inside `Run`.

## Example

With `MaxAttempts = 5`, `BaseDelay = 10ms`, `MaxDelay = 25ms`, and all five attempts
returning a retryable error:

```text
Attempts: 5
Delays:   [10ms, 20ms, 25ms, 25ms]
Error:    giving up after 5 attempts: temporary failure
```

The delay before attempt 2 is 10ms, before attempt 3 is 20ms, and the remaining delays
are capped at 25ms.

## Why this matters

A retry loop that calls `time.Sleep` directly cannot be tested quickly or deterministically.
Injecting the sleeper turns waiting into an observable value, while wrapped errors keep the
failure vocabulary intact for the caller. The policy stays the same in production and in
tests; only the sleeper implementation differs.

## Constraints

- Keep the public types and signatures unchanged.
- Retry only marked errors, never every error.
- Do not sleep after the final attempt.
- Do not normalize or replace a non-retryable error.
- The exhausted error must wrap the last failure with `%w`.
