SELECT
  c.name,
  COUNT(o.id) AS completed_orders,
  COALESCE(SUM(o.total_cents), 0) / 100.0 AS revenue
FROM customers AS c
LEFT JOIN orders AS o
  ON o.customer_id = c.id
 AND o.status = 'completed'
GROUP BY c.id, c.name
ORDER BY revenue DESC, c.name ASC;
