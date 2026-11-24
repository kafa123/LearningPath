def map_failure(error_code: str | None, internal_detail: str | None) -> dict:
    """Return a safe HTTP-facing response for a domain failure code."""
    raise NotImplementedError
