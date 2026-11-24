# Inject a clock into pricing

`PriceCents` accepts the current instant as a parameter, but the implementation ignores it and reads the machine clock instead. As a result, the same input can produce different prices on different days, and the time-based rules cannot be tested reliably.

Replace the ambient time read with the supplied `now` value.

## Function contract

```text
PriceCents(basePriceCents: int, coupon: string?, now: DateTimeOffset) -> int
price_cents(base_price_cents: int, coupon: str | None, now: datetime) -> int
```

`now` is always a UTC instant. Use it for both the day of week and the hour.

## Rules

1. If `basePriceCents <= 0`, return `0`.
2. Trim the coupon and compare it case-insensitively.
3. `WEEKEND10`:
   - applies only on Saturday or Sunday, **and**
   - only when `basePriceCents >= 3000`.
   - The discount is `basePriceCents * 10 / 100`, using integer division.
4. `EARLYBIRD`:
   - applies only when the UTC hour is before `10`, **and**
   - subtracts `500` cents without making the price negative.
5. Unknown or missing coupons return the base price unchanged.
6. Coupons never stack; one coupon is evaluated per call.

## Boundaries

| Instant (UTC) | Rule | Result for a 5000 base |
|---|---|---|
| Sunday 12:00 | `WEEKEND10` | 4500 |
| Saturday 00:00 | `WEEKEND10` | 4500 |
| Monday 12:00 | `WEEKEND10` | 5000 |
| Any day 09:59:59 | `EARLYBIRD` | 4500 |
| Any day 10:00:00 | `EARLYBIRD` | 5000 |

## Why this matters

A function that reads the clock has a hidden input. Passing the instant explicitly makes the dependency visible, lets callers choose the time zone policy, and lets tests cover midnight, weekends, and year changes without waiting for them to happen.

## Constraints

- Do not call `DateTimeOffset.UtcNow`, `DateTime.UtcNow`, `datetime.now`, or any equivalent ambient source.
- Keep the function signature and the return type.
- Keep the integer arithmetic; no floating point.
- Do not special-case individual timestamps.
