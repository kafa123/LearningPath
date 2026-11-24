package main

import (
	"errors"
	"strings"
)

// Item is one inventory ledger row.
type Item struct {
	SKU      string
	Quantity int
	Tags     []string
}

// ErrInvalidSKU reports a SKU that is empty after trimming.
var ErrInvalidSKU = errors.New("invalid sku")

// NormalizeInventory normalizes an inventory ledger.
//
// TODO: merge duplicate SKUs, normalize tags, and sort the result.
func NormalizeInventory(items []Item) ([]Item, error) {
	result := make([]Item, 0, len(items))

	for _, item := range items {
		sku := strings.ToUpper(strings.TrimSpace(item.SKU))
		if sku == "" {
			return nil, ErrInvalidSKU
		}

		if item.Quantity <= 0 {
			continue
		}

		item.SKU = sku
		result = append(result, item)
	}

	return result, nil
}
