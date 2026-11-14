# Completed revenue per customer

The database contains two tables:

```sql
customers (id INTEGER PRIMARY KEY, name TEXT NOT NULL, country TEXT NOT NULL)
orders (
  id INTEGER PRIMARY KEY,
  customer_id INTEGER NOT NULL REFERENCES customers(id),
  status TEXT NOT NULL,
  total_cents INTEGER NOT NULL,
  ordered_at TEXT NOT NULL
)
```

`orders.status` is one of `completed`, `cancelled`, or `pending`. Amounts are
stored as whole cents.

Write one query that returns **every customer** with:

| Column | Meaning |
|---|---|
| `name` | The customer name. |
| `completed_orders` | Number of completed orders for that customer. |
| `revenue` | Completed revenue in dollars (`total_cents / 100.0`). |

Requirements:

- Only `completed` orders contribute to the count and revenue.
- A customer with no completed orders must still appear with `0` and `0.0`.
- Order rows by `revenue` descending, then by `name` ascending.

The result column names must match the table above.

## Example

If Alice has completed orders worth 12000 and 13000 cents, her row is:

```text
name    completed_orders  revenue
Alice   2                 250.0
```

A customer whose orders were all cancelled appears as:

```text
name    completed_orders  revenue
Erin    0                 0.0
```
