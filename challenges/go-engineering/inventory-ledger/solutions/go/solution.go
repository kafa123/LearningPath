package main

import (
	"errors"
	"sort"
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
func NormalizeInventory(items []Item) ([]Item, error) {
	merged := make(map[string]*Item, len(items))
	tagSets := make(map[string]map[string]struct{}, len(items))

	for _, item := range items {
		sku := strings.ToUpper(strings.TrimSpace(item.SKU))
		if sku == "" {
			return nil, ErrInvalidSKU
		}

		if item.Quantity <= 0 {
			continue
		}

		current, ok := merged[sku]
		if !ok {
			current = &Item{SKU: sku, Quantity: item.Quantity}
			merged[sku] = current
			tagSets[sku] = make(map[string]struct{})
		} else if item.Quantity > current.Quantity {
			current.Quantity = item.Quantity
		}

		for _, tag := range item.Tags {
			normalized := strings.TrimSpace(tag)
			if normalized != "" {
				tagSets[sku][normalized] = struct{}{}
			}
		}
	}

	result := make([]Item, 0, len(merged))
	for sku, item := range merged {
		tags := make([]string, 0, len(tagSets[sku]))
		for tag := range tagSets[sku] {
			tags = append(tags, tag)
		}
		sort.Strings(tags)
		item.Tags = tags
		result = append(result, *item)
	}

	sort.Slice(result, func(i, j int) bool {
		return result[i].SKU < result[j].SKU
	})

	return result, nil
}
