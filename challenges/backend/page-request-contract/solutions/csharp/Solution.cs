using System.Globalization;

namespace Challenge;

public sealed record PageResult(
    bool IsValid,
    int Page,
    int PageSize,
    int Offset,
    string? ErrorCode);

public static class Solution
{
    private const int DefaultPage = 1;
    private const int DefaultPageSize = 20;
    private const int MaxPage = 10000;
    private const int MaxPageSize = 100;

    public static PageResult ResolvePage(string? page, string? pageSize)
    {
        var pageValue = DefaultPage;

        if (!string.IsNullOrWhiteSpace(page)
            && (!TryReadInt(page, out pageValue) || pageValue < 1 || pageValue > MaxPage))
        {
            return Invalid("invalid_page");
        }

        var pageSizeValue = DefaultPageSize;

        if (!string.IsNullOrWhiteSpace(pageSize)
            && (!TryReadInt(pageSize, out pageSizeValue)
                || pageSizeValue < 1
                || pageSizeValue > MaxPageSize))
        {
            return Invalid("invalid_page_size");
        }

        return new PageResult(
            true,
            pageValue,
            pageSizeValue,
            (pageValue - 1) * pageSizeValue,
            null);
    }

    private static bool TryReadInt(string raw, out int value)
    {
        return int.TryParse(
            raw.Trim(),
            NumberStyles.None,
            CultureInfo.InvariantCulture,
            out value);
    }

    private static PageResult Invalid(string errorCode)
    {
        return new PageResult(false, 0, 0, 0, errorCode);
    }
}
