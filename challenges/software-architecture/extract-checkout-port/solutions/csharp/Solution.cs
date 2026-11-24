namespace Challenge;

public interface INotifier
{
    void Send(string recipient, string message);
}

public sealed record Order(int Id, int TotalCents, string? Recipient);

public static class Checkout
{
    private const int MinimumTotalCents = 5000;

    public static string Complete(Order order, INotifier notifier)
    {
        if (order.TotalCents <= 0)
        {
            return "empty_order";
        }

        if (string.IsNullOrWhiteSpace(order.Recipient))
        {
            return "invalid_recipient";
        }

        if (order.TotalCents < MinimumTotalCents)
        {
            return "below_threshold";
        }

        notifier.Send(
            order.Recipient!,
            $"Order {order.Id} confirmed: {order.TotalCents} cents");

        return "sent";
    }
}
