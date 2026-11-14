# Refactor Duplicated Discount Rules

Both public functions contain the same pricing rules:

- `standard`: 10% off when the price is at least 100.
- `premium`: 20% off when the price is at least 50.
- Any other tier: no discount.
- Prices below the threshold keep their original value.

The duplication means a rule change must be made twice. Refactor the code so the rule lives in one place, without changing behavior.

## Function contract

```text
ApplyDiscount(price: decimal, tier: string) -> decimal
ApplyDiscountToAll(prices: decimal[], tier: string) -> decimal[]
```

```text
apply_discount(price: float, tier: str) -> float
apply_discount_to_all(prices: list[float], tier: str) -> list[float]
```

## Constraints

- Keep both public signatures unchanged.
- Preserve the threshold comparison: a price exactly at the threshold is discounted.
- Preserve the behavior for unknown tiers.
- Return a new list from the bulk function instead of mutating the input.
- Do not change the discount rates.
