public sealed class UserProfileSpecification : Specification<User, UserProfileResponse>
{
    public UserProfileSpecification(Guid id)
    {
        Query
            .AsNoTracking()
            .Where(u=> u.Id == id);

        Query.Select(u => new UserProfileResponse
        {
            Id = u.Id,
            Name = u.Name,
            Avatar = u.Avatar,
            Email = u.Email,
            Phone = u.Phone,
        });
    }
}