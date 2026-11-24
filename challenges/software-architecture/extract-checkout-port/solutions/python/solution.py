from typing import Protocol

MINIMUM_TOTAL_CENTS = 5000


class Notifier(Protocol):
    def send(self, recipient: str, message: str) -> None:
        ...


def complete(order: dict, notifier: Notifier) -> str:
    if order["totalCents"] <= 0:
        return "empty_order"

    recipient = order.get("recipient")

    if recipient is None or not str(recipient).strip():
        return "invalid_recipient"

    if order["totalCents"] < MINIMUM_TOTAL_CENTS:
        return "below_threshold"

    notifier.send(
        recipient,
        f"Order {order['id']} confirmed: {order['totalCents']} cents",
    )

    return "sent"
