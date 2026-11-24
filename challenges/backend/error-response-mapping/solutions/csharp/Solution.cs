namespace Challenge;

public sealed record FailureResponse(
    int Status,
    string Code,
    string Title,
    string Detail,
    bool Retryable);

public static class Solution
{
    public static FailureResponse MapFailure(string? errorCode, string? internalDetail)
    {
        _ = internalDetail;

        return (errorCode ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "invalid_request" => new FailureResponse(
                400,
                "invalid_request",
                "Invalid request",
                "One or more fields are invalid.",
                false),
            "not_found" => new FailureResponse(
                404,
                "not_found",
                "Resource not found",
                "The requested resource does not exist.",
                false),
            "conflict" => new FailureResponse(
                409,
                "conflict",
                "State conflict",
                "The request conflicts with the current state.",
                false),
            "rate_limited" => new FailureResponse(
                429,
                "rate_limited",
                "Too many requests",
                "Retry after the indicated delay.",
                true),
            "unavailable" => new FailureResponse(
                503,
                "unavailable",
                "Service unavailable",
                "The service is temporarily unavailable.",
                true),
            "timeout" => new FailureResponse(
                504,
                "timeout",
                "Upstream timeout",
                "The dependency did not respond in time.",
                true),
            _ => new FailureResponse(
                500,
                "internal_error",
                "Unexpected error",
                "An unexpected error occurred.",
                false),
        };
    }
}
