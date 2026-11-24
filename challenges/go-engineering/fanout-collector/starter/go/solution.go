package main

import (
	"context"
	"fmt"
)

// Result pairs an input key with its fetched value.
type Result struct {
	Key   string
	Value string
}

// FetchAll fetches every key and returns the values in input order.
func FetchAll(
	ctx context.Context,
	keys []string,
	workers int,
	fetch func(context.Context, string) (string, error),
) ([]Result, error) {
	results := make([]Result, 0, len(keys))

	for _, key := range keys {
		value, err := fetch(ctx, key)
		if err != nil {
			return nil, fmt.Errorf("fetch %s: %w", key, err)
		}
		results = append(results, Result{Key: key, Value: value})
	}

	return results, nil
}
