# Extract a checkout notifier port

`Checkout.Complete` receives a notifier port, but it still writes directly to the console and ignores the port. It also sends confirmations for orders that should be rejected.

Complete the extraction so checkout owns only the checkout rules and delegates delivery to the injected port.

## Public types

```csharp
public interface INotifier
{
    void Send(string recipient, string message);
}

public sealed record Order(int Id, int TotalCents, string? Recipient);

public static class Checkout
{
    public static string Complete(Order order, INotifier notifier);
}
```

```python
class Notifier(Protocol):
    def send(self, recipient: str, message: str) -> None: ...

def complete(order: dict, notifier: Notifier) -> str: ...
```

The Python order uses the keys `id`, `totalCents`, and `recipient`.

## Result codes

Evaluate the rules in this order and return the first matching code:

| Order of checks | Condition | Result | Send? |
|---|---|---|---|
| 1 | `totalCents <= 0` | `empty_order` | no |
| 2 | recipient is null or whitespace-only | `invalid_recipient` | no |
| 3 | `totalCents < 5000` | `below_threshold` | no |
| 4 | otherwise | `sent` | yes, exactly once |

When a qualifying order is sent, the recipient string is passed through as supplied, and the message is exactly:

```text
Order {id} confirmed: {total} cents
```

For example, `Order(42, 5000, "ada@example.com")` sends `"Order 42 confirmed: 5000 cents"`.

## What to change

1. Remove the console write and any concrete gateway call from checkout.
2. Call `notifier` exactly once for a qualifying order.
3. Apply the validation order before any send.
4. Keep the public types and the `Complete`/`complete` signature.

## Why this matters

The checkout rule should not know whether confirmation arrives by email, SMS, or a queue. The port is the boundary: checkout decides *whether* to notify, and the injected implementation decides *how*. That boundary also makes the behavior testable without touching global output.

## Constraints

- Do not add another static dependency to checkout.
- Do not change the message text or the result codes.
- A rejected order must never reach the notifier.
