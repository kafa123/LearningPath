# Map Domain Errors to HTTP Responses

A service layer reports failures with short domain codes. The HTTP boundary must turn each code into a stable status and problem detail without leaking implementation diagnostics.

## Function contract

```text
MapFailure(errorCode: string?, internalDetail: string?) -> FailureResponse
```

```text
map_failure(error_code: str | None, internal_detail: str | None) -> dict
```

`FailureResponse` fields:

| Field | Meaning |
|---|---|
| `Status` / `status` | HTTP status code |
| `Code` / `code` | Stable machine-readable code |
| `Title` / `title` | Short human-readable title |
| `Detail` / `detail` | Safe explanation for the caller |
| `Retryable` / `retryable` | Whether the caller may retry |

## Mapping

| Input code | Status | Output code | Title | Detail | Retryable |
|---|---|---|---|---|---|
| `invalid_request` | 400 | `invalid_request` | Invalid request | One or more fields are invalid. | false |
| `not_found` | 404 | `not_found` | Resource not found | The requested resource does not exist. | false |
| `conflict` | 409 | `conflict` | State conflict | The request conflicts with the current state. | false |
| `rate_limited` | 429 | `rate_limited` | Too many requests | Retry after the indicated delay. | true |
| `unavailable` | 503 | `unavailable` | Service unavailable | The service is temporarily unavailable. | true |
| `timeout` | 504 | `timeout` | Upstream timeout | The dependency did not respond in time. | true |
| anything else | 500 | `internal_error` | Unexpected error | An unexpected error occurred. | false |

## Rules

- Trim the input code and compare it case-insensitively.
- `internalDetail` is diagnostic input only. It must never appear in the output.
- Do not throw for unknown codes.
- A missing or empty code maps to `internal_error`.

## Constraints

- Keep the public function signature.
- Return the canonical title and detail exactly as written in the table.
