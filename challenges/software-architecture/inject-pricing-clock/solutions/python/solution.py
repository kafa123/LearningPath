from datetime import datetime


def price_cents(base_price_cents: int, coupon: str | None, now: datetime) -> int:
    if base_price_cents <= 0:
        return 0

    code = (coupon or "").strip().upper()
    price = base_price_cents

    if code == "WEEKEND10" and now.weekday() >= 5 and price >= 3000:
        price -= price * 10 // 100

    if code == "EARLYBIRD" and now.hour < 10:
        price = max(0, price - 500)

    return price
