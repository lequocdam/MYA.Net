public static class TripCreationPolicy
{
    public static void ValidateOrders(
        IReadOnlyCollection<Order> orders)
    {
        if (orders.Count == 0)
            throw new BadRequestException("No orders found.");

        // Phải cùng warehouse
        if (orders
            .Select(x => x.CurrentWarehouseId)
            .Distinct()
            .Count() > 1)
        {
            throw new BadRequestException("Orders must belong to the same warehouse.");
        }

        // Phải cùng service (nếu business yêu cầu)
        if (orders
            .Select(x => x.ServiceId)
            .Distinct()
            .Count() > 1)
        {
            throw new BadRequestException(
                "Orders must use the same service.");
        }

        // Không vượt giới hạn số lượng
        if (orders.Count > TripConstants.MaxOrdersPerDeliveryTrip)
        {
            throw new BadRequestException(
                $"Maximum {TripConstants.MaxOrdersPerDeliveryTrip} orders per trip.");
        }
    }
}