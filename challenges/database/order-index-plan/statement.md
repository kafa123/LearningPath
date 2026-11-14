# Use the composite order index

The database contains one table and one composite index:

```sql
orders (
  id INTEGER PRIMARY KEY,
  customer_id INTEGER NOT NULL,
  ordered_at TEXT NOT NULL,
  total_cents INTEGER NOT NULL
)

CREATE INDEX idx_orders_customer_date
  ON orders (customer_id, ordered_at);
```

Write one query that returns the orders for customer `42` placed on or after
`2026-01-01`.

| Column | Meaning |
|---|---|
| `id` | Order identifier. |
| `ordered_at` | Order date in `YYYY-MM-DD` form. |
| `total_cents` | Order total in cents. |

Requirements:

- Filter with `customer_id = 42` and `ordered_at >= '2026-01-01'`.
- Order rows by `ordered_at` ascending.
- The result column names must match the table above.

## Performance requirement

The server inspects `EXPLAIN QUERY PLAN` for the submitted query. The plan must:

- use `idx_orders_customer_date`; and
- avoid a full scan of `orders`.

A query that returns the right rows but scans the table does not pass. Keep the
predicate on the indexed columns; wrapping an indexed column in a function or
concatenating a string can prevent the index from being used.
