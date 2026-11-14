# Hints

- The index is ordered by `(customer_id, ordered_at)`. Use equality on `customer_id` and a range on `ordered_at` to get a single index range search.
- Compare `ordered_at` to a date literal such as `'2026-01-01'` directly. Do not call a function on the column.
- `ORDER BY ordered_at` matches the index order for a fixed customer.
- Run `EXPLAIN QUERY PLAN SELECT ...` locally if you want to inspect the plan before submitting.
