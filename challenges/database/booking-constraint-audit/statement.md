# Audit booking constraints

A booking system stores properties, rooms, and reservations. Some rules are enforced by keys and checks. Other rules — especially the overlap rule — cannot be expressed with a simple `UNIQUE` constraint, so the data can still be inconsistent.

Write one query that reports every violation.

## Output contract

Return exactly three columns in this order:

| Column | Meaning |
|---|---|
| `issue` | One of the issue identifiers below |
| `entity_id` | The id of the booking that owns the issue |
| `detail` | A deterministic description of the issue |

Order rows by `issue` ascending, then `entity_id` ascending, then `detail` ascending.

## Issue rules

| Issue | Condition | `detail` |
|---|---|---|
| `invalid_interval` | `starts_at >= ends_at`, regardless of status | `starts_at` + `'..'` + `ends_at` |
| `orphan_booking` | The booking's `room_id` does not exist in `rooms` | The guest email |
| `over_capacity` | Status is not `cancelled` and `party_size > room.capacity` | The room code |
| `overlapping_booking` | Two non-cancelled bookings with valid intervals for the same room overlap | `conflicts with {higher booking id}` |

## Overlap rule

Two intervals overlap when both of these are true:

```text
first.starts_at  <  second.ends_at
second.starts_at <  first.ends_at
```

Touching intervals do not overlap: a booking that ends exactly when the next one starts is valid.

Report each unordered pair once. Use the **lower** booking id as `entity_id` and the higher id in the detail. Do not report the mirror pair.

Only compare bookings that are not cancelled and whose own interval is valid. An invalid interval is already reported by `invalid_interval`.

## Example

Bookings 1 and 2 are confirmed for the same room:

```text
1: 2026-04-01T10:00:00Z .. 2026-04-03T10:00:00Z
2: 2026-04-02T10:00:00Z .. 2026-04-04T10:00:00Z
```

The audit returns:

```text
issue                | entity_id | detail
overlapping_booking  | 1         | conflicts with 2
```

## Constraints

- Use only read-only SQL.
- Do not hard-code ids from the seed data.
- Report capacity equality as valid.
- Return no rows when the data is clean.
