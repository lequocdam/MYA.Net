public sealed class RegistrationRepository(AppDbContext dbContext) : IRegistrationRepository
{
    public async Task AddAsync(
        Registration registration,
        CancellationToken ct)
    {
        await dbContext.Registrations.AddAsync(registration, ct);
    }

    public async Task<Registration?> GetRegistrationByIdAsync(
        Guid id,
        CancellationToken ct)
    {
        return await dbContext.Registrations.FirstOrDefaultAsync(x =>
            x.Id == id, 
            ct);
    }

    public Task<bool> ExistsByEmailAsync(
        string email,
        CancellationToken ct)
    {
        return dbContext.Registrations
            .AnyAsync(
                x => x.Email == email,
                ct);
    }

    public Task<bool> ExistsByPhoneAsync(
        string phone,
        CancellationToken ct)
    {
        return dbContext.Registrations
            .AnyAsync(
                x => x.Phone == phone,
                ct);
    }
}