# Build an LRU cache with generics

A report service caches rendered pages. The cache must be reusable for any comparable key type and any value type, and it must keep the hottest entries by evicting the least recently used one.

## Types and contract

```go
type Cache[K comparable, V any] struct {
    // unexported state
}

func NewCache[K comparable, V any](capacity int) *Cache[K, V]
func (c *Cache[K, V]) Get(key K) (V, bool)
func (c *Cache[K, V]) Put(key K, value V)
func (c *Cache[K, V]) Remove(key K)
func (c *Cache[K, V]) Len() int
func (c *Cache[K, V]) Keys() []K
```

## Rules

- `NewCache` clamps a `capacity` below 1 to 1.
- `Get` returns the stored value and `true` on a hit, and the zero value for `V` plus `false` on a miss.
- A successful `Get` refreshes the key's recency.
- `Put` on an existing key updates the value and refreshes recency without evicting another key.
- `Put` on a new key inserts it as most recently used. When the cache is full, it evicts the least recently used key first.
- `Remove` deletes a key and is a no-op when the key is absent.
- `Len` reports the number of stored entries.
- `Keys` reports keys from most recently used to least recently used.
- `Keys` on an empty cache returns an empty non-nil `[]K`.

## Examples

```go
cache := NewCache[string, int](2)
cache.Put("a", 1)
cache.Put("b", 2)
cache.Get("a")   // 1, true; "a" is now most recently used
cache.Put("c", 3) // evicts "b"
cache.Keys()      // []string{"c", "a"}
```

```go
cache := NewCache[string, int](2)
value, ok := cache.Get("missing")
// value == 0, ok == false
```

```go
cache := NewCache[string, int](0) // capacity is clamped to 1
cache.Put("a", 1)
cache.Put("b", 2) // evicts "a"
cache.Keys()      // []string{"b"}
```

## Expected operations

Operations are applied in order. Every operation produces exactly one observation:

| Operation | Observation |
|---|---|
| `Put` | `{"op": "put"}` |
| `Get` | `{"op": "get", "found": bool, "value": V}` |
| `Remove` | `{"op": "remove"}` |
| `Keys` | `{"op": "keys", "keys": []K}` |
| `Len` | `{"op": "len", "len": int}` |

## Constraints

- Keep the public names and method signatures.
- A hit must not change the stored value, only the recency.
- A miss must not insert, evict, or refresh anything.
- Do not return `nil` from `Keys`.
- The cache must work for string and integer keys and for numeric, string, and struct values.
