# Hints

- Check the guard order before writing any send call. The first failing rule wins.
- `string.IsNullOrWhiteSpace` covers null, empty, and blank recipients in one check. In Python, `recipient is None or not str(recipient).strip()` behaves the same.
- Pass the recipient through unchanged; the contract only rejects a blank value, it does not normalize a valid one.
- The threshold is inclusive: 5000 qualifies, 4999 does not.
- Keep the message interpolated exactly once. A second send, even with identical text, fails the single-send test.
