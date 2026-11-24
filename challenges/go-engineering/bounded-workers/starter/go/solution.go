package main

import (
	"context"
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

// Process runs every job through a bounded pool and returns outcomes in input
// order.
func Process(
	ctx context.Context,
	jobs []Job,
	workers int,
	handle func(context.Context, Job) (int, error),
) ([]Outcome, error) {
	var mu sync.Mutex
	var wait sync.WaitGroup
	var firstErr error
	outcomes := []Outcome{}

	for _, job := range jobs {
		wait.Add(1)
		go func(job Job) {
			defer wait.Done()

			value, err := handle(ctx, job)
			mu.Lock()
			defer mu.Unlock()
			if err != nil {
				if firstErr == nil {
					firstErr = err
				}
				return
			}
			outcomes = append(outcomes, Outcome{ID: job.ID, Value: value})
		}(job)
	}

	wait.Wait()

	if firstErr != nil {
		return nil, firstErr
	}
	return outcomes, nil
}
