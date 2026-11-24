# Normalize an inventory ledger

An inventory import arrives as a slice of rows. SKUs are inconsistently cased and padded, some rows repeat a SKU, and tags are noisy. Normalize the ledger into a deterministic slice that keeps the most useful quantity and the union of every row's tags.

## Function contract

```go
type Item struct {
    SKU      string
    Quantity int
    Tags     []string
}

var ErrInvalidSKU = errors.New("invalid sku")

func NormalizeInventory(items []Item) ([]Item, error)
```

## Rules

Apply the rules in this order:

| Order | Rule | Result |
|---|---|---|
| 1 | Trim whitespace from `SKU`, then uppercase it. | Normalized SKU |
| 2 | If the normalized SKU is empty, reject the call. | `nil, ErrInvalidSKU` |
| 3 | Drop rows whose `Quantity` is less than or equal to zero. | Row omitted |
| 4 | Merge rows that share a normalized SKU. | Highest quantity wins |
| 5 | Union the tags of every merged row. | Trimmed, deduplicated, sorted |
| 6 | Sort the result by SKU ascending. | Deterministic order |

Precedence: the SKU check happens before the quantity check. A row with an empty SKU returns `ErrInvalidSKU` even when its quantity would have caused the row to be dropped.

### Tags

- Trim each tag and discard tags that are empty after trimming.
- Remove exact duplicate tags.
- Sort the remaining tags ascending.
- Every returned item has a non-nil `Tags` slice. An item with no tags returns an empty slice, not `nil`.

### Empty input

An empty input returns an empty non-nil `[]Item` and a `nil` error.

### No mutation

`NormalizeInventory` must not modify the input slice, any `Item` in it, or the backing array of any `Tags` slice. The tests keep a deep copy of the input and compare it after the call.

## Examples

```go
items := []Item{
    {SKU: " b ", Quantity: 2, Tags: []string{" x ", "x"}},
    {SKU: "a", Quantity: 1, Tags: []string{}},
}
NormalizeInventory(items)
// []Item{
//     {SKU: "A", Quantity: 1, Tags: []string{}},
//     {SKU: "B", Quantity: 2, Tags: []string{"x"}},
// }
```

```go
items := []Item{
    {SKU: "a", Quantity: 2, Tags: []string{"z"}},
    {SKU: " A ", Quantity: 5, Tags: []string{"a", "z"}},
}
NormalizeInventory(items)
// []Item{{SKU: "A", Quantity: 5, Tags: []string{"a", "z"}}}
```

```go
items := []Item{{SKU: "   ", Quantity: 3, Tags: []string{}}}
_, err := NormalizeInventory(items)
// err is ErrInvalidSKU
```

## Constraints

- Keep the public type, sentinel, and function signature.
- Use `errors.Is` compatibility for `ErrInvalidSKU`; return it directly or wrap it.
- Return `nil` for the result when the call fails.
- Do not sort or trim inside the input's tag slices.
