public sealed class UserDetailSpecification : Specification<User, UserResponse>
{
    public UserDetailSpecification(Guid id)
    {
        Query
            .AsNoTracking()
            .Where(x => x.Id == id);

        Query.Select(x => new UserResponse
        {
            Id = x.Id,
            Name = x.Name,
            Email = x.Email,
            Phone = x.Phone,
            CreatedAt = x.CreatedAt
        });
    }
}