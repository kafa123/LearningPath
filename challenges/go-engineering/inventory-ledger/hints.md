# Hints

- Normalize the SKU once at the top of the loop and use the result for the map key.
- Validate the SKU before you look at `Quantity`; the documented precedence matters.
- Copy tags into a fresh slice (or a set) instead of sorting `item.Tags` directly. Sorting in place mutates the caller's backing array.
- Group rows in a `map[string]*Item`, then copy the values into a new slice and sort it.
- A local `map[string]map[string]struct{}` is a simple way to union tags per SKU.
- Build every tag slice with `make([]string, 0, n)` so items with no tags marshal as `[]` rather than `null`.
- Check the empty case: `make([]Item, 0)` is non-nil, while `var result []Item` is not.
