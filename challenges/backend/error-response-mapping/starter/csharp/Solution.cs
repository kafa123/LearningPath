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
        throw new NotImplementedException();
    }
}
