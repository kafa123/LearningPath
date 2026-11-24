using System;

namespace Challenge;

public static class Solution
{
    public static int PriceCents(int basePriceCents, string? coupon, DateTimeOffset now)
    {
        if (basePriceCents <= 0)
        {
            return 0;
        }

        var code = (coupon ?? string.Empty).Trim().ToUpperInvariant();
        var price = basePriceCents;

        if (code == "WEEKEND10"
            && (now.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            && price >= 3000)
        {
            price -= price * 10 / 100;
        }

        if (code == "EARLYBIRD" && now.Hour < 10)
        {
            price = Math.Max(0, price - 500);
        }

        return price;
    }
}
