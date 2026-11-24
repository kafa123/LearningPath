# Hints

- Validate `size` before you loop. Returning `nil, ErrInvalidBatchSize` is a separate branch from the empty input.
- Use `size` as the step and clamp the end index to `len(items)`. Do not build batches by appending one element at a time.
- `len(items) == 0` should leave you with `make([][]T, 0)`, not a nil slice.
- In `GroupBy`, call `key(item)` once and store the result in a local variable before appending.
- A single pass with `groups[k] = append(groups[k], item)` already preserves order inside each group.
- Start `Sum` with `var total T`; the zero value is correct for every numeric type in the constraint.
- Do not read `items` through `unsafe` or reflect. The type parameters are enough.
