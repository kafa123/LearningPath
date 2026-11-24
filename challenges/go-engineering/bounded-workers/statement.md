# Process jobs with a bounded worker pool

A batch processor must run many jobs with a fixed amount of concurrency, keep the results aligned with the input, and stop promptly when one job fails.

## API contract

```go
type Job struct {
	ID      string
	Payload int
}

type Outcome struct {
	ID    string
	Value int
}

func Process(
	ctx context.Context,
	jobs []Job,
	workers int,
	handle func(context.Context, Job) (int, error),
) ([]Outcome, error)
```

`handle` runs one job and returns its value.

## Rules

- `workers < 1` behaves like `1`. A worker count larger than `len(jobs)` is allowed.
- Return one `Outcome` per input job, in input order. Duplicate ids stay duplicated.
- Every job is handled at most once, and at most `workers` `handle` calls run at the same time.
- When `handle` fails, cancel outstanding work through a derived context, wait for every started goroutine to exit, and return `nil` plus `fmt.Errorf("job %s: %w", job.ID, err)`.
- If the caller's context is already canceled, return `nil, ctx.Err()` without calling `handle`.
- If the caller's context is canceled and no job failed first, return `nil, ctx.Err()` after cleanup.
- Empty input returns an empty, non-nil `[]Outcome` and no goroutines.

## Example

```go
jobs := []Job{{ID: "a", Payload: 3}, {ID: "b", Payload: 4}}
outcomes, err := Process(ctx, jobs, 2, handle)
// outcomes == []Outcome{{ID: "a", Value: ...}, {ID: "b", Value: ...}}
```

## Harness cases

Each case supplies `jobs`, `workers`, and optional switches: `values` (the value `handle` returns per id), `blockingIds`, `errorJobId` and `errorAfterAllStarted`, `canceled`, `cancelAfterStarted`, `holdStarted`, and `reverse`. It expects `results` (or `null`), `error` (`""`, `"job"`, or `"context canceled"`), an optional exact `errorText`, `calls`, `maxConcurrency`, and `canceledHandles`.

`calls` counts `handle` invocations, `maxConcurrency` is the peak number of simultaneous invocations, and `canceledHandles` counts handlers that observed cancellation. Do not depend on goroutine scheduling for correctness; coordinate through the context and ordered result slots.
