SELECT
  id,
  ordered_at,
  total_cents
FROM orders
WHERE customer_id = 42
  AND ordered_at >= '2026-01-01'
ORDER BY ordered_at;
