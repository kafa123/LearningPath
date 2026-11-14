CREATE TABLE orders (
  id INTEGER PRIMARY KEY,
  customer_id INTEGER NOT NULL,
  ordered_at TEXT NOT NULL,
  total_cents INTEGER NOT NULL CHECK (total_cents >= 0)
);

CREATE INDEX idx_orders_customer_date
  ON orders (customer_id, ordered_at);
