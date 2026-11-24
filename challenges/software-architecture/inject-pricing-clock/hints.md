# Hints

- The parameter is already plumbed through. Use `now.DayOfWeek` and `now.Hour` in C#, or `now.weekday()` and `now.hour` in Python.
- `DateTimeOffset` parsed from an instant ending in `Z` is already UTC, so `Hour` is the UTC hour. Do not convert it again.
- Python's `weekday()` returns Monday as `0` and Sunday as `6`, so `now.weekday() >= 5` means weekend.
- The hour check is strict: `now.Hour < 10`. At exactly `10:00:00` the coupon no longer applies.
- Keep the discount in integers. `price * 10 / 100` floors; `Math.Round` and Python's `round` do not.
- Clamp the early-bird result with `Math.Max(0, ...)` or `max(0, ...)`.
