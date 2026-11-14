CREATE TABLE customers (
  id INTEGER PRIMARY KEY,
  name TEXT NOT NULL,
  country TEXT NOT NULL
);

CREATE TABLE orders (
  id INTEGER PRIMARY KEY,
  customer_id INTEGER NOT NULL REFERENCES customers(id),
  status TEXT NOT NULL CHECK (status IN ('completed', 'cancelled', 'pending')),
  total_cents INTEGER NOT NULL CHECK (total_cents >= 0),
  ordered_at TEXT NOT NULL
);
