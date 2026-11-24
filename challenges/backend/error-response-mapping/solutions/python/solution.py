def map_failure(error_code: str | None, internal_detail: str | None) -> dict:
    _ = internal_detail
    code = (error_code or "").strip().lower()

    if code == "invalid_request":
        return _response(400, "invalid_request", "Invalid request", "One or more fields are invalid.", False)

    if code == "not_found":
        return _response(404, "not_found", "Resource not found", "The requested resource does not exist.", False)

    if code == "conflict":
        return _response(409, "conflict", "State conflict", "The request conflicts with the current state.", False)

    if code == "rate_limited":
        return _response(429, "rate_limited", "Too many requests", "Retry after the indicated delay.", True)

    if code == "unavailable":
        return _response(503, "unavailable", "Service unavailable", "The service is temporarily unavailable.", True)

    if code == "timeout":
        return _response(504, "timeout", "Upstream timeout", "The dependency did not respond in time.", True)

    return _response(500, "internal_error", "Unexpected error", "An unexpected error occurred.", False)


def _response(status: int, code: str, title: str, detail: str, retryable: bool) -> dict:
    return {
        "status": status,
        "code": code,
        "title": title,
        "detail": detail,
        "retryable": retryable,
    }
