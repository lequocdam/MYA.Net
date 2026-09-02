public class UserProjectionSpecification : Specification<User, UserResponse>
{
    public UserProjectionSpecification(UserQueryParams query) : base(query)
    {
        Query.Select(x => new UserResponse
        {
            Id = x.Id,
            Name = x.Name,
            Email = x.Email,
            Phone = x.Phone,

            Roles = x.UserRoles
                .Select(r=>r.Role.Code)
                .ToList(),

            Status = x.Status
        });

        Query.Skip((query.Page-1) * query.PageSize);
        
        Query.Take(query.PageSize);
    }
}