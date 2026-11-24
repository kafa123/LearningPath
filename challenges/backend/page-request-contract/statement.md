# Validate a Page Request Contract

An endpoint accepts `page` and `pageSize` query values. Before the values reach a data access layer, normalize them into a paging window. Invalid input must produce a field-level error instead of an exception or an empty page.

## Function contract

```text
ResolvePage(page: string?, pageSize: string?) -> PageResult
```

```text
resolve_page(page: str | None, page_size: str | None) -> dict
```

`PageResult` has these fields:

| Field | Meaning |
|---|---|
| `IsValid` / `isValid` | `false` when any input violates the contract |
| `Page` / `page` | Normalized page number |
| `PageSize` / `pageSize` | Normalized page size |
| `Offset` / `offset` | `(page - 1) * pageSize` |
| `ErrorCode` / `errorCode` | `null`, `"invalid_page"`, or `"invalid_page_size"` |

## Rules

- A value that is `null`, empty, or whitespace uses its default: `page = 1`, `pageSize = 20`.
- Otherwise the trimmed value must contain only ASCII digits. Leading zeros are allowed.
- `page` must be between 1 and 10000 inclusive.
- `pageSize` must be between 1 and 100 inclusive.
- Validate `page` first. If both values are invalid, return `invalid_page`.
- An invalid result uses `page = 0`, `pageSize = 0`, `offset = 0`, and a non-null error code.
- Never throw for invalid input.

## Example

```json
{
  "page": "3",
  "pageSize": "25",
  "expected": {
    "isValid": true,
    "page": 3,
    "pageSize": 25,
    "offset": 50,
    "errorCode": null
  }
}
```

## Constraints

- Keep the public function signature.
- Do not use exceptions for expected validation failures.
- Do not clamp an out-of-range value into range; reject it.
