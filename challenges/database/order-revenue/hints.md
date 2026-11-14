# Hints

- Start from `customers` and use a `LEFT JOIN` so customers without orders are preserved.
- Put the status filter in the `ON` clause of the join. A `WHERE o.status = 'completed'` filter would remove customers whose joined row is `NULL`.
- `COUNT(o.id)` ignores `NULL`, so it counts only completed orders.
- `COALESCE(SUM(o.total_cents), 0)` gives zero for a customer with no completed orders.
- Divide by `100.0`, not `100`, to keep a decimal result.
