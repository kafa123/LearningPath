# Hints

- `strings.TrimSpace(email) == ""` is the simplest blank-recipient check in Go.
- Wrap with `fmt.Errorf("...: %w", err)`; `errors.Is` walks that chain. `%v` would flatten it.
- The check order is part of the contract. Return immediately after the first failure.
- Build the success message once, pass it to `Send`, and return the same string in the result.
- On every failure return `Result{}` rather than a partially filled result.
- The repository fake returns sentinels that the harness compares with `errors.Is`, so a
  wrapped repository error must keep the original error in its chain.
