SELECT id, actor, kind, occurred_at
FROM events
WHERE tenant_id = 7
  AND occurred_at < '2026-03-01T00:00:00Z'
ORDER BY occurred_at DESC, id DESC
LIMIT 5;
