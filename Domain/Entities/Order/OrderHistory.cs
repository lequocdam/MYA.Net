public class OrderHistory
{
    public Guid Id { get; private set; }
    public DateTime Date { get; private set; }
    public string Note { get; private set; }
    public OrderStatus Status { get; private set; }
    public Guid UserId { get; private set; }
    public Guid OrderId { get; private set; }

    public static OrderHistory Create(
        OrderStatus status,
        Guid userId,
        Guid orderId)
    {
        return new Order
        {
            Id = Guid.NewGuid(),
            Date = DateTime.UtcNow,
            Note = "",
            Status = status,
            UserId = userId,
            OrderId = orderId,
        };
    }
}