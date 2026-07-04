using Ardalis.Specification;

public sealed class LoadOrdersSpec : Specification<Order>
{
    public LoadOrdersSpec(IEnumerable<Guid> orderIds)
    {
        Query.Where(o => orderIds.Contains(o.Id));
    }
}