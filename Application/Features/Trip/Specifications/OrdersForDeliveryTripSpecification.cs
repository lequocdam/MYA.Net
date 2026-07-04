public sealed class OrdersReadyForDeliverySpecification : Specification<Order>
{
    public OrdersReadyForDeliverySpec(IEnumerable<Guid> orderIds)
    {
        Query.Where(x => orderIds.Contains(x.Id));

        Query.Where(x => x.Status == OrderStatus.Ready);

        Query.Include(x => x.FromAddress);

        Query.Include(x => x.ToAddress);
    }
}