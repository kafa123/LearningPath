# Observe requests with injected time

A service needs one header on every response: how long the handler took, in whole milliseconds. The middleware must not guess the time itself, and it must not lose the status or body the downstream handler already produced.

## API contract

```go
type Clock interface {
	Now() time.Time
}

func Observe(clock Clock, next http.Handler) http.Handler
```

`Observe` returns a handler that wraps `next`.

## Rules

- Call `clock.Now()` exactly twice per request: once before `next` runs, once after it returns.
- Compute `elapsed := after.Sub(before)` and set `X-Elapsed-Ms` to `elapsed.Milliseconds()` in decimal. A sub-millisecond duration reports `0`.
- Preserve the downstream status code, response headers, and body byte for byte.
- The downstream handler may write its status and body immediately, so a header added after `next` returns would be too late. Buffer the downstream response and write it once the elapsed header is known.
- Pass the original `*http.Request` to `next` unchanged so request-scoped values stay visible.
- Never call `time.Now`, `time.Since`, or `time.Sleep`. The injected clock is the only source of time.
- A downstream handler that never writes a status still means `200`.

## Example

```go
before := time.Date(2024, 5, 1, 10, 0, 0, 0, time.UTC)
after := before.Add(250 * time.Millisecond)
```

A downstream handler that writes `200` and `{"ok":true}` must reach the client with status `200`, body `{"ok":true}`, and `X-Elapsed-Ms: 250`.

## Harness cases

Each case supplies `before` and `after` timestamps (RFC 3339), the downstream `status` and `body`, and a `contextValue`. It expects `status`, `elapsedHeader`, `body`, `clockCalls`, and `contextSeen`. `clockCalls` must be `2`, and `contextSeen` is true only when the downstream handler observed the injected request value.
