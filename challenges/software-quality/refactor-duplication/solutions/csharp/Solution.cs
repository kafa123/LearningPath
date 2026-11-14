namespace Challenge;

public static class Solution
{
    public static decimal ApplyDiscount(decimal price, string tier)
    {
        return DiscountedPrice(price, tier);
    }

    public static decimal[] ApplyDiscountToAll(decimal[] prices, string tier)
    {
        var result = new decimal[prices.Length];

        for (var index = 0; index < prices.Length; index++)
        {
            result[index] = DiscountedPrice(prices[index], tier);
        }

        return result;
    }

    private static decimal DiscountedPrice(decimal price, string tier)
    {
        if (tier == "standard")
        {
            return price >= 100m ? price * 0.9m : price;
        }

        if (tier == "premium")
        {
            return price >= 50m ? price * 0.8m : price;
        }

        return price;
    }
}
