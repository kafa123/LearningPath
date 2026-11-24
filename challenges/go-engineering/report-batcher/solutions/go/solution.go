package main

import "errors"

// ErrInvalidBatchSize reports a batch size below 1.
var ErrInvalidBatchSize = errors.New("invalid batch size")

// Number is the numeric constraint used by Sum.
type Number interface {
	~int | ~int64 | ~float64
}

// Batch splits items into consecutive batches of at most size elements.
func Batch[T any](items []T, size int) ([][]T, error) {
	if size <= 0 {
		return nil, ErrInvalidBatchSize
	}

	batches := make([][]T, 0, (len(items)+size-1)/size)

	for start := 0; start < len(items); start += size {
		end := start + size
		if end > len(items) {
			end = len(items)
		}

		batch := make([]T, end-start)
		copy(batch, items[start:end])
		batches = append(batches, batch)
	}

	return batches, nil
}

// GroupBy groups items by the value returned from key.
func GroupBy[T any, K comparable](items []T, key func(T) K) map[K][]T {
	groups := make(map[K][]T)

	for _, item := range items {
		k := key(item)
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
