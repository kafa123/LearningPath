SELECT issue, entity_id, detail
FROM (
  SELECT 'invalid_interval' AS issue,
         b.id AS entity_id,
         b.starts_at || '..' || b.ends_at AS detail
  FROM bookings AS b
  WHERE b.starts_at >= b.ends_at

  UNION ALL

  SELECT 'orphan_booking',
         b.id,
         b.guest_email
  FROM bookings AS b
  WHERE NOT EXISTS (
    SELECT 1 FROM rooms AS r WHERE r.id = b.room_id
  )

  UNION ALL

  SELECT 'over_capacity',
         b.id,
         r.code
  FROM bookings AS b
  JOIN rooms AS r ON r.id = b.room_id
  WHERE b.status <> 'cancelled'
    AND b.party_size > r.capacity

  UNION ALL

  SELECT 'overlapping_booking',
         first_booking.id,
         'conflicts with ' || second_booking.id
  FROM bookings AS first_booking
  JOIN bookings AS second_booking
    ON second_booking.room_id = first_booking.room_id
   AND second_booking.id > first_booking.id
   AND second_booking.status <> 'cancelled'
   AND first_booking.status <> 'cancelled'
   AND first_booking.starts_at < first_booking.ends_at
   AND second_booking.starts_at < second_booking.ends_at
   AND first_booking.starts_at < second_booking.ends_at
   AND second_booking.starts_at < first_booking.ends_at
)
ORDER BY issue ASC, entity_id ASC, detail ASC;
