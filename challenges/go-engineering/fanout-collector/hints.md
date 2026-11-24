# Hints

- Write results into a slice index that comes from the input position, not from the
  order in which workers finish.
- A simple pool is a jobs channel, `workers` goroutines ranging over it, and a
  `sync.WaitGroup`. Add the producer to the same `WaitGroup` so `FetchAll` cannot return
  while it is still sending.
- Derive `workCtx, cancel := context.WithCancel(ctx)` and call `cancel` as soon as the
  first real error is recorded.
- When a worker's fetch returns `ctx.Err()` because the pool canceled it, do not overwrite
  the first recorded failure.
- Check `ctx.Err()` before launching the pool. That is the only way to guarantee zero
  fetch calls for an already-canceled context.
- Clamp `workers` to the number of keys after the empty-input check; spawning a million
  goroutines for three keys is still a bug.
- `make([]Result, len(keys))` is already non-nil when `len(keys) == 0`, so empty input
  needs no special allocation beyond that.
