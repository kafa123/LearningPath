def cart_total(lines: list[dict], coupon: str | None) -> int:
    subtotal = 0

    for line in lines:
        quantity = line["quantity"]

        if quantity <= 0:
            continue

        subtotal += line["unitPriceCents"] * quantity

    code = (coupon or "").strip().upper()
    discount = 0

    if code == "SAVE10" and subtotal >= 5000:
        discount = subtotal * 10 // 100

    if code == "SAVE25" and subtotal >= 10000:
        discount = subtotal * 25 // 100

    return subtotal - discount
