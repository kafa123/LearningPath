CREATE TABLE events (
  id INTEGER PRIMARY KEY,
  tenant_id INTEGER NOT NULL,
  actor TEXT NOT NULL,
  kind TEXT NOT NULL,
  occurred_at TEXT NOT NULL
);

CREATE INDEX idx_events_tenant_occurred
  ON events (tenant_id, occurred_at DESC, id DESC);
