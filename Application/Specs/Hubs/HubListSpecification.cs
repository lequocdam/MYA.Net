using Ardalis.Specification;

public sealed class HubListSpecification : Specification<Hub, HubResponse>
{
    public HubListSpecification(GetListRequest request)
    {
        Query.AsNoTracking();

        ApplySearch(request);

        ApplySort();

        Query.Select(h => new HubResponse
        {
            Id = h.Id,
            Code = h.Code,
            Name = h.Name,
            Address = h.Address,
            CreatedAt = h.CreatedAt
        });

        Query.Skip((request.Page - 1) * request.PageSize);
        Query.Take(request.PageSize);
    }

    private void ApplySearch(GetListRequest request)
    {
        Query.Where(h =>
            h.Code.Contains(keyword) ||
            h.Name.Contains(keyword));
    }

    private void ApplySort()
    {
        Query
        .OrderByDescending(h => h.CreatedAt)
        .ThenByDescending(h => h.Id);
    }
}