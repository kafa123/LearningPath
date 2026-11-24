def cart_total(lines: list[dict], coupon: str | None) -> int:
    subtotal = 0

    for line in lines:
        subtotal += line["unitPriceCents"] * line["quantity"]

    code = (coupon or "").upper()
    discount = 0

    if code == "SAVE10" and subtotal > 5000:
        discount = round(subtotal * 0.10)

    if code == "SAVE25" and subtotal > 10000:
        discount = round(subtotal * 0.25)

    return subtotal - discount
