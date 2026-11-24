package main

import (
	"context"
	"fmt"
	"sync"
)

// Job is one unit of work for the pool.
type Job struct {
	ID      string
	Payload int
}

// Outcome pairs an input job with the value its handler produced.
type Outcome struct {
	ID    string
	Value int
}

type workerJob struct {
	index int
	job   Job
}

// Process runs every job through a bounded pool and returns outcomes in input
// order.
func Process(
	ctx context.Context,
	jobs []Job,
	workers int,
	handle func(context.Context, Job) (int, error),
) ([]Outcome, error) {
	if err := ctx.Err(); err != nil {
		return nil, err
	}
	if len(jobs) == 0 {
		return []Outcome{}, nil
	}
	if workers < 1 {
		workers = 1
	}
	if workers > len(jobs) {
		workers = len(jobs)
	}

	workCtx, cancel := context.WithCancel(ctx)
	defer cancel()

	outcomes := make([]Outcome, len(jobs))
	queue := make(chan workerJob)

	var wait sync.WaitGroup
	var mu sync.Mutex
	var firstErr error
	var failedID string

	wait.Add(workers + 1)

	worker := func() {
		defer wait.Done()
		for item := range queue {
			select {
			case <-workCtx.Done():
				return
			default:
			}

			value, err := handle(workCtx, item.job)
			if err != nil {
				if workCtx.Err() != nil {
					return
				}
				mu.Lock()
				if firstErr == nil {
					firstErr = err
					failedID = item.job.ID
				}
				mu.Unlock()
				cancel()
				return
			}

			outcomes[item.index] = Outcome{ID: item.job.ID, Value: value}
		}
	}

	for i := 0; i < workers; i++ {
		go worker()
	}

	go func() {
		defer wait.Done()
		defer close(queue)
		for index, job := range jobs {
			select {
			case <-workCtx.Done():
				return
			case queue <- workerJob{index: index, job: job}:
			}
		}
	}()

	wait.Wait()

	if firstErr != nil {
		return nil, fmt.Errorf("job %s: %w", failedID, firstErr)
	}
	if err := workCtx.Err(); err != nil {
		return nil, err
	}

	return outcomes, nil
}
