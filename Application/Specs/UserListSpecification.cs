using Ardalis.Specification;

public sealed class UserListSpecification : Specification<User, UserResponse>
{
    public UserListSpecification(GetUsersRequest request)
    {
        Query.AsNoTracking();

        ApplySearch(request);

        ApplyFilter(request);

        ApplySort();

        Query.Select(x => new UserResponse
        {
            Id = x.Id,
            Name = x.Name,
            Email = x.Email,
            Phone = x.Phone,
            CreatedAt = x.CreatedAt
        });

        Query.Skip((request.Page - 1) * request.PageSize);
        Query.Take(request.PageSize);
    }

    private void ApplySearch(GetUsersRequest request)
    {
        if(!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var keyword = request.Keyword.Trim();

            Query.Where(x =>
                x.Name.Contains(keyword) ||
                x.Email.Contains(keyword) ||
                x.Phone.Contains(keyword));
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