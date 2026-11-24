# Hints

- A map gives O(1) lookup. A second structure must track recency independently of the map.
- The standard `container/list` package works, or you can keep your own doubly linked list with `head` and `tail`.
- Store the key in the recency node so eviction can delete the matching map entry.
- `Get` on a hit performs the same move-to-front step as a `Put` on an existing key.
- `Put` on a new key inserts at the front, then evicts `tail` only when `len(entries) > capacity`.
- Uncapping a node must update both neighbours and both `head`/`tail` pointers.
- Build `Keys` with `make([]K, 0, len(entries))`; a nil slice fails the empty-cache case.
- Do not use the map's iteration order for recency. It is deliberately randomized.
