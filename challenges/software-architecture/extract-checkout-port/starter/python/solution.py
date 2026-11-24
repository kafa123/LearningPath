from typing import Protocol


class Notifier(Protocol):
    def send(self, recipient: str, message: str) -> None:
        ...


def complete(order: dict, notifier: Notifier) -> str:
    # TODO: checkout still prints directly instead of using the notifier port,
    # and it does not enforce the order rules.
    print(f"Order {order['id']} confirmed: {order['totalCents']} cents")
    return "sent"
