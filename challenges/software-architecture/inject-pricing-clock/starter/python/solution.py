from datetime import datetime, timezone


def price_cents(base_price_cents: int, coupon: str | None, now: datetime) -> int:
    if base_price_cents <= 0:
        return 0

    code = (coupon or "").strip().upper()
    price = base_price_cents

    # TODO: this reads the ambient clock instead of the supplied instant.
    current = datetime.now(timezone.utc)

    if code == "WEEKEND10" and current.weekday() >= 5 and price >= 3000:
        price -= price * 10 // 100

    if code == "EARLYBIRD" and current.hour < 10:
        price = max(0, price - 500)

    return price
