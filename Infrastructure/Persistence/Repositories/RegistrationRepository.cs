public class RegistrationRepository(AppDbContext context) : RepositoryBase<Registration>, IRegistrationRepository
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

    public async Task<Registration?> GetByIdAsync(
        Guid id,
        CancellationToken ct)
    {
        return await context.Registrations.FirstOrDefaultAsync(r => r.Id == id, ct);
    }

    public async Task<bool> ExistAsync(
        string email,
        string phone,
        CancellationToken ct)
    {
        return await context.Registrations.AnyAsync(u =>
            u.Email == email ||
            u.Phone == phone,
            ct);
    }

    public async Task<Registration?> GetByContactAsync(
        string email,
        string phone,
        CancellationToken ct)
    {
        return await dbContext.Registrations.FirstOrDefaultAsync(x =>
            x.Status == RegistrationStatus.Pending &&
                (x.Email == email || x.Phone == phone),
            ct);
    }

    public async Task AddAsync(Registration registration, CancellationToken ct)
    {
        await context.Registrations.AddAsync(registration, ct);
    }

    public async Task<int> DeleteByPendingAsync(DateTime cutoff, CancellationToken ct)
    {
        return await context.Registrations
            .Where(x => x.Status == RegistrationStatus.Pending && r.CreatedAt < cutoff)
            .ExecuteDeleteAsync(ct); // EF Core 7+, không cần load entity, chạy trực tiếp SQL DELETE
    }
}