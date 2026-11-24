package main

// Cache is a typed least-recently-used cache.
type Cache[K comparable, V any] struct {
	capacity int
	entries  map[K]V
	order    []K
}

// NewCache creates a cache with the supplied capacity.
func NewCache[K comparable, V any](capacity int) *Cache[K, V] {
	if capacity < 1 {
		capacity = 1
	}

	return &Cache[K, V]{
		capacity: capacity,
		entries:  make(map[K]V),
	}
}

// Get returns the value for key and whether it was present.
//
// TODO: refresh recency on a hit.
func (c *Cache[K, V]) Get(key K) (V, bool) {
	value, ok := c.entries[key]
	return value, ok
}

// Put stores value under key.
//
// TODO: track recency and evict the least recently used key at capacity.
func (c *Cache[K, V]) Put(key K, value V) {
	if _, ok := c.entries[key]; ok {
		c.entries[key] = value
		return
	}

	if len(c.entries) >= c.capacity {
		oldest := c.order[0]
		c.order = c.order[1:]
		delete(c.entries, oldest)
	}

	c.entries[key] = value
	c.order = append(c.order, key)
}

// Remove deletes key when it is present.
func (c *Cache[K, V]) Remove(key K) {
	if _, ok := c.entries[key]; !ok {
		return
	}

	delete(c.entries, key)
	for i, existing := range c.order {
		if existing == key {
			c.order = append(c.order[:i], c.order[i+1:]...)
			break
		}
	}
}

// Len reports the number of stored entries.
func (c *Cache[K, V]) Len() int {
	return len(c.entries)
}

// Keys reports keys from most recently used to least recently used.
//
// TODO: keep insertion order in sync with reads.
func (c *Cache[K, V]) Keys() []K {
	keys := make([]K, 0, len(c.order))
	for i := len(c.order) - 1; i >= 0; i-- {
		keys = append(keys, c.order[i])
	}
	return keys
}
