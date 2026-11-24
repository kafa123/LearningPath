# Hints

- Treat each rule as a separate `SELECT` and combine them with `UNION ALL`, then sort once in the outer query.
- For `invalid_interval`, compare the text timestamps directly. ISO 8601 UTC strings sort chronologically, so `starts_at >= ends_at` is enough.
- Use `NOT EXISTS` for orphaned bookings. Keep `over_capacity` as an inner join so a missing room cannot silently satisfy the capacity comparison.
- For overlaps, join `bookings` to itself on `room_id` and add `second.id > first.id`. That single predicate removes mirrored pairs.
- Both interval rules matter: `first.starts_at < second.ends_at AND second.starts_at < first.ends_at`. Using `<=` would incorrectly flag touching bookings.
- Filter `status <> 'cancelled'` on both sides of the overlap join, and require both intervals to be valid.
- Build the detail text with `||`. Cast the id as text if your database requires it; SQLite concatenates integers automatically.
