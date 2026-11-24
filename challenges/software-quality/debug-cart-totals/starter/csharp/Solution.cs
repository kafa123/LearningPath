namespace Challenge;

public sealed record CartLine(int UnitPriceCents, int Quantity);

public static class Solution
{
    public static int CartTotal(IReadOnlyList<CartLine> lines, string? coupon)
    {
        var subtotal = 0;

        foreach (var line in lines)
        {
            subtotal += line.UnitPriceCents * line.Quantity;
        }

        var code = (coupon ?? string.Empty).ToUpperInvariant();
        var discount = 0;

        if (code == "SAVE10" && subtotal > 5000)
        {
            discount = (int)Math.Round(subtotal * 0.10);
        }

        if (code == "SAVE25" && subtotal > 10000)
        {
            discount = (int)Math.Round(subtotal * 0.25);
        }

        return subtotal - discount;
    }
}
