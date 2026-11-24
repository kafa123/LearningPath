# Hints

- A middleware is a function from `http.Handler` to `http.Handler`, so return `http.HandlerFunc` and call `next.ServeHTTP` inside it.
- `http.ResponseWriter` writes headers when the first byte is flushed. Capture the downstream status, headers, and body in your own writer, then copy them to the real one.
- Implement `Header`, `Write`, and `WriteHeader` on the buffering type. `Write` implies `200` when no status was written.
- Read `clock.Now()` once before and once after `next`; do not call it a third time.
- `time.Duration.Milliseconds()` truncates, so 400 microseconds is `0`.
- Keep the same `*http.Request` when calling `next`; a new `context.Background()` drops the caller's values.
