-- Return one row per customer with completed order count and revenue.
-- Replace this query with your solution.
SELECT
  c.name,
  0 AS completed_orders,
  0.0 AS revenue
FROM customers AS c
ORDER BY c.name;
