# Serve orders with net/http

An order service exposes two small HTTP routes over a storage port. The handler owns HTTP semantics: routing, payload validation, status codes, and context propagation. The store owns persistence.

## API contract

```go
type Order struct {
	ID         string `json:"id"`
	TotalCents int    `json:"totalCents"`
}

type Store interface {
	Get(ctx context.Context, id string) (Order, bool, error)
	Save(ctx context.Context, order Order) error
}

func NewHandler(store Store) http.Handler
```

## Routes

| Method | Path | Meaning |
|---|---|---|
| GET | `/orders/{id}` | Read one order |
| POST | `/orders` | Create an order |

Routing uses `http.ServeMux` method patterns. Unknown paths return the mux default `404`; a known path with an unsupported method returns the mux default `405`. Neither default body is part of the contract.

## Responses

Every JSON response sets `Content-Type: application/json`.

| Situation | Status | Body | Location |
|---|---|---|---|
| GET, found | 200 | the order | absent |
| GET, missing | 404 | `{"error":"not_found"}` | absent |
| GET, store error | 500 | `{"error":"internal_error"}` | absent |
| POST, success | 201 | the stored order | `/orders/{id}` |
| POST, invalid input | 400 | `{"error":"invalid_request"}` | absent |
| POST, save error | 500 | `{"error":"internal_error"}` | absent |

A POST body is invalid when it is malformed JSON, contains more than one JSON value, has an empty id after trimming, or has `totalCents <= 0`. Invalid input must be rejected before the store is called.

## Rules

- Pass `r.Context()` to every `Store` call. The caller may attach request-scoped values that the store must observe.
- Trim the id once, then validate and store the trimmed value. The response body and `Location` use the trimmed id.
- Do not call `Save` for a rejected payload.
- Use the decoder's end-of-input check to detect trailing JSON values.

## Harness cases

Each case supplies `method`, `path`, `body`, and `contextValue`, and expects `status`, `body`, `location`, `storeCalls`, and optionally `contentType` and `contextSeen`. `storeCalls` counts every `Get` and `Save` call for the case; `contextSeen` is true only when at least one call observed the injected request value.
