# Hints

- Treat null, empty, and whitespace as "not supplied".
- Parse the trimmed value deliberately. `int.TryParse` with `NumberStyles.None` rejects signs and surrounding spaces; a regular expression such as `^[0-9]+$` is the Python equivalent.
- Choose a single error code per result. Check `page` before `pageSize`.
- Construct the success result only after both values pass validation.
