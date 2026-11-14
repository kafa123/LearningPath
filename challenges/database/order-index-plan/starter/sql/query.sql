-- Return customer 42 orders on or after 2026-01-01 using the composite index.
-- Replace this query with your solution.
SELECT
  id,
  ordered_at,
  total_cents
FROM orders
WHERE customer_id = 42
ORDER BY ordered_at;
