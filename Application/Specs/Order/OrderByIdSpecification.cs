public sealed class ByIdSpecification : Specification<Order>
{
    public ByIdSpecification(Guid id)
    {
        Query.Where(x => x.Id == id);
        
        Query.Include(x => x.FromAddress);
        Query.Include(x => x.ToAddress);
        Query.Include(x => x.Trackings);

        Query.AsNoTracking();
    }
}