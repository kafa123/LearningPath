def apply_discount(price: float, tier: str) -> float:
    return _discounted_price(price, tier)


def apply_discount_to_all(prices: list[float], tier: str) -> list[float]:
    return [_discounted_price(price, tier) for price in prices]


def _discounted_price(price: float, tier: str) -> float:
    if tier == "standard":
        return price * 0.9 if price >= 100 else price

    if tier == "premium":
        return price * 0.8 if price >= 50 else price

    return price
