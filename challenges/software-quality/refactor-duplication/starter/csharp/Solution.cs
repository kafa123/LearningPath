namespace Challenge;

public static class Solution
{
    public static decimal ApplyDiscount(decimal price, string tier)
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

    public static decimal[] ApplyDiscountToAll(decimal[] prices, string tier)
    {
        var result = new decimal[prices.Length];

        for (var index = 0; index < prices.Length; index++)
        {
            var price = prices[index];

            if (tier == "standard")
            {
                result[index] = price >= 100m ? price * 0.9m : price;
            }
            else if (tier == "premium")
            {
                result[index] = price >= 50m ? price * 0.8m : price;
            }
            else
            {
                result[index] = price;
            }
        }

        return result;
    }
}
