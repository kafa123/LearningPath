INSERT INTO customers (id, name, country) VALUES
  (1, 'Alice', 'US'),
  (2, 'Bob', 'DE'),
  (3, 'Carol', 'US'),
  (4, 'Dave', 'JP'),
  (5, 'Erin', 'US');

INSERT INTO orders (id, customer_id, status, total_cents, ordered_at) VALUES
  (1, 1, 'completed', 12000, '2026-01-02'),
  (2, 1, 'completed', 13000, '2026-02-11'),
  (3, 2, 'completed', 10000, '2026-01-15'),
  (4, 3, 'cancelled', 5000, '2026-01-20'),
  (5, 3, 'completed', 7550, '2026-03-01'),
  (6, 4, 'pending', 2000, '2026-02-05'),
  (7, 5, 'cancelled', 3000, '2026-03-04');
