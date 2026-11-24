# Hints

- A worker pool has three parts: a fixed set of workers, a job queue, and a way to know when the queue is drained.
- The dispatcher goroutine owns the queue channel and closes it when it stops sending. Workers only receive.
- Give each job its input index so a worker can write the result into `outcomes[index]` without a shared append.
- Count the dispatcher in the same `sync.WaitGroup` as the workers; closing the queue and waiting for workers are two different steps.
- Derive a cancellable context from the caller's context. On the first handler error, store the error once, then cancel.
- Select on `ctx.Done()` when sending to the queue, otherwise a full pool and a canceled request can block forever.
- Check `ctx.Err()` after waiting: it distinguishes a canceled caller from a failing job.
- Do not start goroutines for an empty job list.
