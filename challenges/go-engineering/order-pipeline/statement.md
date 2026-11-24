# Fulfill orders through ports

`Fulfill` must load an order through a repository port, apply the fulfillment rules, and
send one confirmation through a notifier port. The starter version calls the ports but
drops the wrapping and treats a blank recipient as valid.

Finish the pipeline so every failure is inspectable with `errors.Is` and no rule leaks
past an injected interface.

## Public API

```go
type Order struct {
	ID         string
	TotalCents int
	Email      string
}

type Repository interface {
	Load(id string) (Order, error)
}

type Notifier interface {
	Send(message string) error
}

type Result struct {
	Sent    bool
	Message string
}

var ErrNotFound = errors.New("order not found")
var ErrInvalidRecipient = errors.New("invalid recipient")
var ErrBelowMinimum = errors.New("order below minimum")

func Fulfill(repo Repository, notifier Notifier, id string) (Result, error)
```

Implement `Fulfill` in `solution.go`. Do not add a `main` function; the server-side
harness supplies the entry point.

## Rules

Evaluate the checks in this order and stop at the first failure:

| Order | Condition | Return |
|---|---|---|
| 1 | `repo.Load` returns an error | `Result{}`, `fmt.Errorf("load order %s: %w", id, err)` |
| 2 | `strings.TrimSpace(order.Email) == ""` | `Result{}`, `fmt.Errorf("order %s: %w", id, ErrInvalidRecipient)` |
| 3 | `order.TotalCents < 1000` | `Result{}`, `fmt.Errorf("order %s: %w", id, ErrBelowMinimum)` |
| 4 | `notifier.Send` returns an error | `Result{}`, `fmt.Errorf("send confirmation for %s: %w", id, err)` |
| 5 | otherwise | `Result{Sent: true, Message: message}`, `nil` |

The success message is exactly:

```text
Order {id} confirmed: {total} cents
```

`{id}` is the `id` argument to `Fulfill`, and `{total}` is `order.TotalCents`. Send it
exactly once through `notifier.Send`, then return the same text in `Result.Message`.

## What to change

1. Wrap the repository error so `errors.Is` reaches the sentinel the repository returned.
2. Reject empty and whitespace-only recipients before checking the total.
3. Wrap the two validation sentinels with the order id.
4. Wrap the notifier error so `errors.Is` reaches the notifier sentinel.
5. On every failure return the zero `Result{}` and never send a confirmation.

## Why this matters

The caller on the other side of `Fulfill` should not need the concrete repository or
notifier types. The interfaces define the seam, and wrapped sentinels preserve the
failure vocabulary across that seam. That combination is what makes the rule testable
with fakes and diagnosable in production without string matching.

## Constraints

- Keep the public types, sentinels, and `Fulfill` signature unchanged.
- Use `%w` and not `%v` when wrapping.
- A load failure must win over a validation failure, and validation must win over delivery.
- A rejected order must never reach the notifier.
- The total threshold is inclusive: 1000 cents qualifies, 999 does not.
