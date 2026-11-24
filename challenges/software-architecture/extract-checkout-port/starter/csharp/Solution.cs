using System;

namespace Challenge;

public interface INotifier
{
    void Send(string recipient, string message);
}

public sealed record Order(int Id, int TotalCents, string? Recipient);

public static class Checkout
{
    public static string Complete(Order order, INotifier notifier)
    {
        // TODO: checkout still writes directly instead of using the notifier port,
        // and it does not enforce the order rules.
        Console.WriteLine($"Order {order.Id} confirmed: {order.TotalCents} cents");
        return "sent";
    }
}
