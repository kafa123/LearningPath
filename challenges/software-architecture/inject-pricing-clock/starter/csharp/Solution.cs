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

        // TODO: this reads the ambient clock instead of the supplied instant.
        var current = DateTimeOffset.UtcNow;

        if (code == "WEEKEND10"
            && (current.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            && price >= 3000)
        {
            price -= price * 10 / 100;
        }

        if (code == "EARLYBIRD" && current.Hour < 10)
        {
            price = Math.Max(0, price - 500);
        }

        return price;
    }
}
