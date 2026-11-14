# Add Request Validation to an Endpoint

The `POST /registrations` endpoint currently accepts any payload. Add validation before the request is processed.

## Request contract

```json
{
  "email": "ada@example.com",
  "displayName": "Ada",
  "age": 36
}
```

| Field | Rule |
|---|---|
| `email` | Required, at most 254 characters, must contain `@` |
| `displayName` | Required, trimmed length between 2 and 50 |
| `age` | Between 13 and 120 inclusive |

## Responses

- **201 Created** when every field is valid.
- **400 Bad Request** with field-level errors when one or more fields are invalid.

Use `Results.ValidationProblem` so the response is a consistent ProblemDetails payload. Field keys should match the JSON property names: `email`, `displayName`, and `age`.

## Why this matters

A handler that trusts input pushes the failure downstream. Validation at the boundary makes the contract explicit, gives the caller an actionable error, and prevents invalid state from being stored.

## Constraints

- Keep the route and the `RegistrationRequest` record unchanged.
- Validate all fields; do not stop after the first error.
- Do not use exceptions for expected validation failures.
