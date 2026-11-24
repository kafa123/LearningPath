package main

import (
	"context"
	"fmt"
	"sync"
)

// Result pairs an input key with its fetched value.
type Result struct {
	Key   string
	Value string
}

type fetchJob struct {
	index int
	key   string
}

// FetchAll fetches every key and returns the values in input order.
func FetchAll(
	ctx context.Context,
	keys []string,
	workers int,
	fetch func(context.Context, string) (string, error),
) ([]Result, error) {
	if err := ctx.Err(); err != nil {
		return nil, err
	}
	if len(keys) == 0 {
		return []Result{}, nil
	}
	if workers < 1 {
		workers = 1
	}
	if workers > len(keys) {
		workers = len(keys)
	}

	workCtx, cancel := context.WithCancel(ctx)
	defer cancel()

	results := make([]Result, len(keys))
	jobs := make(chan fetchJob)

	var wait sync.WaitGroup
	var mu sync.Mutex
	var firstErr error
	var failedKey string

	wait.Add(workers + 1)

	worker := func() {
		defer wait.Done()
		for job := range jobs {
			select {
			case <-workCtx.Done():
				return
			default:
			}

			value, err := fetch(workCtx, job.key)
			if err != nil {
				if workCtx.Err() != nil {
					return
				}
				mu.Lock()
				if firstErr == nil {
					firstErr = err
					failedKey = job.key
				}
				mu.Unlock()
				cancel()
				return
			}

			results[job.index] = Result{Key: job.key, Value: value}
		}
	}

	for i := 0; i < workers; i++ {
		go worker()
	}

	go func() {
		defer wait.Done()
		defer close(jobs)
		for index, key := range keys {
			select {
			case <-workCtx.Done():
				return
			case jobs <- fetchJob{index: index, key: key}:
			}
		}
	}()

	wait.Wait()

	if firstErr != nil {
		return nil, fmt.Errorf("fetch %s: %w", failedKey, firstErr)
	}
	if err := workCtx.Err(); err != nil {
		return nil, err
	}

	return results, nil
}
