# Build a tenant activity feed

The `events` table records activity for many tenants. A dashboard shows the latest activity for **tenant 7**, but only events that happened before `2026-03-01T00:00:00Z`. The feed shows at most five rows.

The table already has this index:

```sql
CREATE INDEX idx_events_tenant_occurred
  ON events (tenant_id, occurred_at DESC, id DESC);
```

Write a query that uses it.

## Output contract

Return exactly these columns in this order:

| Column | Meaning |
|---|---|
| `id` | Event id |
| `actor` | Who performed the action |
| `kind` | Event type |
| `occurred_at` | ISO 8601 UTC timestamp |

Rules:

- Include only rows whose `tenant_id` is `7`.
- Exclude rows at or after the cutoff. The comparison is strictly `<`.
- Order by `occurred_at` descending. When two events share a timestamp, the larger `id` comes first.
- Return at most five rows.
- Do not scan the whole `events` table.

## Why the order matters

The index covers the tenant filter, the timestamp range, and the descending order. A matching query reads only the rows it needs and stops after the limit. Adding a different sort expression, or filtering with a function on `occurred_at`, can force the planner to scan and sort instead.

## Constraints

- Use only read-only SQL.
- Do not hard-code event ids.
- The query must keep using the index even when no rows match.
