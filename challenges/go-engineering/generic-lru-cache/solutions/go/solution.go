package main

type cacheNode[K comparable, V any] struct {
	key   K
	value V
	prev  *cacheNode[K, V]
	next  *cacheNode[K, V]
}

// Cache is a typed least-recently-used cache.
type Cache[K comparable, V any] struct {
	capacity int
	entries  map[K]*cacheNode[K, V]
	head     *cacheNode[K, V]
	tail     *cacheNode[K, V]
}

// NewCache creates a cache with the supplied capacity.
func NewCache[K comparable, V any](capacity int) *Cache[K, V] {
	if capacity < 1 {
		capacity = 1
	}

	return &Cache[K, V]{
		capacity: capacity,
		entries:  make(map[K]*cacheNode[K, V]),
	}
}

// Get returns the value for key and whether it was present.
func (c *Cache[K, V]) Get(key K) (V, bool) {
	node, ok := c.entries[key]
	if !ok {
		var zero V
		return zero, false
	}

	c.moveToFront(node)
	return node.value, true
}

// Put stores value under key.
func (c *Cache[K, V]) Put(key K, value V) {
	if node, ok := c.entries[key]; ok {
		node.value = value
		c.moveToFront(node)
		return
	}

	node := &cacheNode[K, V]{key: key, value: value}
	c.entries[key] = node
	c.pushFront(node)

	if len(c.entries) > c.capacity {
		least := c.tail
		if least != nil {
			c.unlink(least)
			delete(c.entries, least.key)
		}
	}
}

// Remove deletes key when it is present.
func (c *Cache[K, V]) Remove(key K) {
	node, ok := c.entries[key]
	if !ok {
		return
	}

	c.unlink(node)
	delete(c.entries, key)
}

// Len reports the number of stored entries.
func (c *Cache[K, V]) Len() int {
	return len(c.entries)
}

// Keys reports keys from most recently used to least recently used.
func (c *Cache[K, V]) Keys() []K {
	keys := make([]K, 0, len(c.entries))
	for node := c.head; node != nil; node = node.next {
		keys = append(keys, node.key)
	}
	return keys
}

func (c *Cache[K, V]) pushFront(node *cacheNode[K, V]) {
	node.prev = nil
	node.next = c.head

	if c.head != nil {
		c.head.prev = node
	}
	c.head = node

	if c.tail == nil {
		c.tail = node
	}
}

func (c *Cache[K, V]) unlink(node *cacheNode[K, V]) {
	if node.prev != nil {
		node.prev.next = node.next
	} else {
		c.head = node.next
	}

	if node.next != nil {
		node.next.prev = node.prev
	} else {
		c.tail = node.prev
	}

	node.prev = nil
	node.next = nil
}

func (c *Cache[K, V]) moveToFront(node *cacheNode[K, V]) {
	if c.head == node {
		return
	}

	c.unlink(node)
	c.pushFront(node)
}
