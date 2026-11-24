namespace Challenge;

public sealed record PageResult(
    bool IsValid,
    int Page,
    int PageSize,
    int Offset,
    string? ErrorCode);

public static class Solution
{
    public static PageResult ResolvePage(string? page, string? pageSize)
    {
        throw new NotImplementedException();
    }
}
