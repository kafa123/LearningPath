package main

import "errors"

// ErrInvalidBatchSize reports a batch size below 1.
var ErrInvalidBatchSize = errors.New("invalid batch size")

// Number is the numeric constraint used by Sum.
type Number interface {
	~int | ~int64 | ~float64
}

// Batch splits items into consecutive batches of at most size elements.
//
// TODO: validate size and return a non-nil empty result for empty input.
func Batch[T any](items []T, size int) ([][]T, error) {
	var batches [][]T

	if size <= 0 {
		return nil, nil
	}

	for start := 0; start < len(items); start += size {
		end := start + size
		if end > len(items) {
			end = len(items)
		}
		batches = append(batches, items[start:end])
	}

	return batches, nil
}

// GroupBy groups items by the value returned from key.
//
// TODO: return a non-nil empty map for empty input.
func GroupBy[T any, K comparable](items []T, key func(T) K) map[K][]T {
	var groups map[K][]T

	for _, item := range items {
		k := key(item)
		if groups == nil {
			groups = make(map[K][]T)
		}
		groups[k] = append(groups[k], item)
	}

	return groups
}

// Sum adds every value in the slice.
func Sum[T Number](values []T) T {
	var total T
	for _, value := range values {
		total += value
	}
	return total
}
