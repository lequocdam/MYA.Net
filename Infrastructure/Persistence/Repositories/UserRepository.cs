public class UserRepository(AppDbContext context) : RepositoryBase<User>, IUserRepository
{
    public async Task<PagedResult<UserResponse>> GetUsersAsync(
        GetUsersRequest request,
        CancellationToken ct)
    {
        var items = await ListAsync(new UserListSpecification(request);, ct);

        var count = await CountAsync(new UserCountSpecification(request);, ct);

        return new PagedResult<UserResponse>
        {
            Items = items,
            TotalCount = count,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    public async Task<UserResponse?> GetUserByIdAsync(
        Guid id,
        CancellationToken ct)
    {
        return await FirstOrDefaultAsync(new UserDetailSpecification(id), ct);
    }

    public async Task<UserResponse?> GetByContactAsync(
        string email,
        string phone,
        CancellationToken ct)
    {
        return await dbContext.Users.FirstOrDefaultAsync(x =>
            x.Email == email ||
            x.Phone == phone,
            ct);
    }

    public async Task<bool> ExistAsync(
        string email,
        string phone,
        CancellationToken ct)
    {
        return await context.Users.AnyAsync(u =>
            u.Email == email ||
            u.Phone == phone,
            ct);
    }

    public async Task AddAsync(
        User user,
        CancellationToken ct)
    {
        await context.Users.AddAsync(user, ct);
    }
}