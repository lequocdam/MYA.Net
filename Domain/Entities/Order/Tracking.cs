public class Tracking
{
    public Guid Id { get; private set; }
    public DateTime Date { get; private set; }
    public string Message { get; private set; }
    public OrderStatus Status { get; private set; }
    public Guid OrderId { get; private set; }

    public static Tracking Create(
        OrderStatus status,
        Guid orderId)
    {
        return new Order
        {
            Id = Guid.NewGuid(),
            Date = DateTime.UtcNow,
            Message = "",
            Status = status,
            OrderId = orderId,
        };
    }

    private static string GetMessage(OrderStatus status) => status switch
    {
        OrderStatus.Pending         => "Đơn hàng đang chờ xác nhận",
        OrderStatus.Confirmed       => "Đơn hàng đã được xác nhận",
        OrderStatus.PickingUp       => "Đang lấy hàng từ người gửi",
        OrderStatus.PickedUp        => "Đã lấy hàng thành công",
        OrderStatus.Transiting      => "Hàng đang trên đường trung chuyển",
        OrderStatus.Arrived         => "Hàng đã về kho đích",
        OrderStatus.Delivering      => "Đơn hàng đang trên đường giao đến bạn",
        OrderStatus.Completed       => "Giao hàng thành công",
        OrderStatus.Failed          => "Giao hàng thất bại",
        OrderStatus.ReturnRequested => "Đang xử lý yêu cầu hoàn hàng",
        OrderStatus.Returning       => "Hàng đang được hoàn về người gửi",
        OrderStatus.Returned        => "Hoàn hàng thành công",
        OrderStatus.Cancelled       => "Đơn hàng đã bị hủy",
        _                           => status.ToString()
    };
}