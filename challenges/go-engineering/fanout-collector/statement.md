# Fan out requests with bounded concurrency

`FetchAll` receives a list of keys and a fetch function that may block or fail. It must
run fetches concurrently, but never more than `workers` at a time, and it must return the
values in input order. The starter fetches one key at a time, ignores cancellation, and
never consults the worker limit.

Implement the bounded fan-out with a derived context so the first failure stops the work
cleanly.

## Public API

```go
type Result struct {
	Key   string
	Value string
}

func FetchAll(
	ctx context.Context,
	keys []string,
	workers int,
	fetch func(context.Context, string) (string, error),
) ([]Result, error)
```

Implement `FetchAll` in `solution.go`. Do not add a `main` function; the server-side
harness supplies the entry point.

## Rules

1. `workers < 1` clamps to `1`. `workers` may exceed `len(keys)`.
2. Preserve input order in the returned `[]Result`. Duplicate keys stay duplicated, and
   each input element is fetched at most once.
3. At most `workers` calls to `fetch` may be in flight at the same time. Do not start one
   goroutine per key; use a worker pool or a semaphore sized to the limit.
4. When a fetch fails, cancel outstanding work through a context derived from `ctx`, wait
   for every started goroutine to exit, and return:
   ```go
   nil, fmt.Errorf("fetch %s: %w", key, err)
   ```
   The first failure decides the returned key and error.
5. If `ctx` is already canceled, return `nil, ctx.Err()` and do not call `fetch`.
6. Empty `keys` returns an empty, non-nil `[]Result` and a `nil` error.
7. On success, every returned value corresponds to the key at the same input position.
8. Never use `time.Sleep` to coordinate the pool. Use channels, `sync.WaitGroup`, and
   context cancellation.

## Example

```go
results, err := FetchAll(ctx, []string{"alpha", "beta", "gamma"}, 2, fetch)
```

With all fetches succeeding, `results[0].Key == "alpha"`, `results[1].Key == "beta"`, and
`results[2].Key == "gamma"`, regardless of which fetch finished first. If the fetch for
`"beta"` fails with `errUpstream`, the call returns a `nil` slice and an error that
satisfies `errors.Is(err, errUpstream)` and starts with `fetch beta:`.

## Why this matters

Fan-out is easy; bounded fan-out with deterministic ordering and clean shutdown is not.
The worker limit protects the downstream service, the indexed result slice removes any
dependence on completion order, and the derived context plus a `WaitGroup` guarantee that
no fetch outlives the call. That is also what makes the function safe under the race
detector.

## Constraints

- Keep the `Result` type and `FetchAll` signature unchanged.
- Respect the worker limit for the whole call, not just the first batch.
- Return `nil` results on any error, including an already-canceled context.
- Do not lose duplicate keys or reorder results.
- Wait for all started goroutines before returning.
