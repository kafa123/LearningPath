-- Report booking constraint violations.
SELECT issue, entity_id, detail
FROM (
  SELECT 'invalid_interval' AS issue,
         id AS entity_id,
         starts_at || '..' || ends_at AS detail
  FROM bookings
  WHERE starts_at >= ends_at
);
