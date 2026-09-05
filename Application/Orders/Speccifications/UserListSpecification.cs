using Ardalis.Specification;

public sealed class OrderListSpecification : Specification<Order, OrderResponse>
{
    public OrderListSpecification(GetListRequest request)
    {
        Query.AsNoTracking();
        ApplySearch(request);
        ApplyFilter(request);
        ApplySort();

        Query.Select(o => new OrderResponse
        {
            Id = o.Id,
            UserId = o.UserId,
            Email = x.Email,
            Phone = x.Phone,
            CreatedAt = x.CreatedAt
        });

        Query.Skip((request.Page - 1) * request.PageSize);
        Query.Take(request.PageSize);
    }

    private void ApplySearch(GetListRequest request)
    {
        if(!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var keyword = request.Keyword.Trim();

            Query.Where(o =>
                o.Name.Contains(keyword) ||
                o.Email.Contains(keyword) ||
                o.Phone.Contains(keyword));
        }
    }

    private void ApplyFilter(GetUsersRequest request)
    {
        if(request.Status.HasValue)
        {
            Query.Where(x => x.Status == query.Status);
        }
    }

    private void ApplySort()
    {
        Query
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id);
    }
}