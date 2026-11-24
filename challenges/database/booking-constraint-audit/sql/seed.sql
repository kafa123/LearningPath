INSERT INTO properties (id, name) VALUES
  (1, 'Harbor'),
  (2, 'Downtown');

INSERT INTO rooms (id, property_id, code, capacity) VALUES
  (1, 1, 'A1', 2),
  (2, 1, 'A2', 4),
  (3, 2, 'B1', 3);

INSERT INTO bookings
  (id, room_id, guest_email, party_size, starts_at, ends_at, status)
VALUES
  (1, 1, 'ada@example.com', 2, '2026-04-01T10:00:00Z', '2026-04-03T10:00:00Z', 'confirmed'),
  (2, 1, 'grace@example.com', 2, '2026-04-02T10:00:00Z', '2026-04-04T10:00:00Z', 'confirmed'),
  (3, 2, 'linus@example.com', 6, '2026-04-05T10:00:00Z', '2026-04-06T10:00:00Z', 'confirmed'),
  (4, 3, 'omar@example.com', 2, '2026-04-07T10:00:00Z', '2026-04-07T09:00:00Z', 'cancelled'),
  (5, 99, 'pia@example.com', 1, '2026-04-08T10:00:00Z', '2026-04-09T10:00:00Z', 'held');
