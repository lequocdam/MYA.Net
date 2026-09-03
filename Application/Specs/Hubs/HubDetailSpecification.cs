public sealed class HubDetailSpecification : Specification<Hub, HubResponse>
{
    public HubDetailSpecification(Guid id)
    {
        Query
            .AsNoTracking()
            .Where(h => h.Id == id);

        Query.Select(x => new HubResponse
        {
            Id = h.Id,
            Code = h.Code,
            Name = h.Name,
            Address = h.Address,
            CreatedAt = h.CreatedAt
        });
    }
}