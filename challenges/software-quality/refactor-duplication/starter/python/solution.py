def apply_discount(price: float, tier: str) -> float:
    if tier == "standard":
        return price * 0.9 if price >= 100 else price

    if tier == "premium":
        return price * 0.8 if price >= 50 else price

    return price


def apply_discount_to_all(prices: list[float], tier: str) -> list[float]:
    result: list[float] = []

    for price in prices:
        if tier == "standard":
            result.append(price * 0.9 if price >= 100 else price)
        elif tier == "premium":
            result.append(price * 0.8 if price >= 50 else price)
        else:
            result.append(price)

    return result
