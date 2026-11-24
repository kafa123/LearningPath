# Debug cart totals and coupons

The cart calculator works for simple orders but fails for several real inputs. The defects are independent, so a fix for one case may not reveal the next.

## Function contract

```text
CartTotal(lines: CartLine[], coupon: string?) -> int
cart_total(lines: list[dict], coupon: str | None) -> int
```

A line has two values:

| Field | Meaning |
|---|---|
| `UnitPriceCents` / `unitPriceCents` | Price of one unit, in integer cents |
| `Quantity` / `quantity` | Number of units on the line |

## Rules

1. Ignore every line whose quantity is zero or negative.
2. The subtotal is the sum of `unitPriceCents * quantity` for the remaining lines.
3. Trim the coupon and compare it case-insensitively.
4. `SAVE10` applies only when `subtotal >= 5000`. The discount is `subtotal * 10 / 100` using integer division.
5. `SAVE25` applies only when `subtotal >= 10000`. The discount is `subtotal * 25 / 100` using integer division.
6. The total is `subtotal - discount`.
7. An unknown coupon, an empty coupon, or a cart with no billable lines leaves the total unchanged. An empty cart totals `0`.

## Example

```json
{
  "lines": [{ "unitPriceCents": 1111, "quantity": 5 }],
  "coupon": "SAVE10",
  "expected": 5000
}
```

The subtotal is `5555`. The discount is `5555 * 10 / 100 = 555` (floored), so the total is `5000`. Rounding the discount to `556` would produce the wrong answer.

## Constraints

- Work in integer cents. Do not use floating point for money or discounts.
- Keep both public function signatures.
- Fix the rules, not individual test inputs.
- Do not special-case coupon names beyond the two documented codes.
