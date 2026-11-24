# Hints

- The predicate order should match the index: `tenant_id` equality first, then the `occurred_at` range.
- Use a literal timestamp comparison, not `date(occurred_at)` or `substr(occurred_at, ...)`; wrapping the column prevents an index search.
- `ORDER BY occurred_at DESC, id DESC` matches the index definition, so the planner can avoid a separate sort.
- `LIMIT 5` lets SQLite stop after five matching rows.
- Inspect `EXPLAIN QUERY PLAN` while you work: you want `USING INDEX idx_events_tenant_occurred` and no `SCAN events`.
