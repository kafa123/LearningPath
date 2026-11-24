# Batch and group report rows

A reporting pipeline needs three reusable helpers. Each one is generic, so the same code works for strings, structs, and numeric values.

## Function contract

```go
var ErrInvalidBatchSize = errors.New("invalid batch size")

type Number interface {
    ~int | ~int64 | ~float64
}

func Batch[T any](items []T, size int) ([][]T, error)
func GroupBy[T any, K comparable](items []T, key func(T) K) map[K][]T
func Sum[T Number](values []T) T
```

## Rules

### Batch

- A `size` below 1 returns `nil, ErrInvalidBatchSize`, even when `items` is empty.
- An empty input with a valid size returns an empty non-nil `[][]T`.
- Preserve input order. Every batch except the last has exactly `size` elements, and the last batch may be shorter.
- Do not modify the input slice.

### GroupBy

- Call `key` exactly once per item. Tests count the calls.
- Preserve the input order inside every group.
- An empty input returns an empty non-nil `map[K][]T`.
- Do not modify the input slice.

### Sum

- Return the zero value for an empty slice.
- Work for `int`, `int64`, and `float64` through the `Number` constraint.
- Accumulate in `T` so the numeric type is preserved.

## Examples

```go
Batch([]string{"a", "b", "c", "d", "e"}, 2)
// [][]string{{"a", "b"}, {"c", "d"}, {"e"}}
```

```go
GroupBy([]string{"bee", "ant", "bat", "ape"}, func(s string) string { return s[:1] })
// map[string][]string{
//     "b": {"bee", "bat"},
//     "a": {"ant", "ape"},
// }
```

```go
Sum([]float64{1.5, 2.25, 0.25})
// 4
```

## Constraints

- Keep the public names and signatures exactly as declared.
- `Batch` must return `nil` for the slice when it returns `ErrInvalidBatchSize`.
- Use `errors.Is` compatibility for `ErrInvalidBatchSize`.
- Build results with `make` so empty collections are non-nil.
- Do not use reflection or type switches to implement the numeric sum.
