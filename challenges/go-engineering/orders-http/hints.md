# Hints

- `http.NewServeMux` plus the Go 1.22 patterns `"GET /orders/{id}"` and `"POST /orders"` gives you `r.PathValue("id")` and the standard 404/405 behavior for free.
- Decode once with `json.NewDecoder(r.Body).Decode(&order)`, then call `Decode` again and require `io.EOF` to reject a second JSON value. Any other result means extra tokens or trailing garbage.
- Trim with `strings.TrimSpace` before checking the id, and store the trimmed value so the body and `Location` agree.
- Set `Location` before calling `WriteHeader(201)`; headers written after the status line are ignored.
- Keep a small `writeJSON` helper that sets `Content-Type: application/json`, calls `WriteHeader`, and encodes the value.
- Passing `context.Background()` compiles but fails the propagation cases; use `r.Context()` in both routes.
