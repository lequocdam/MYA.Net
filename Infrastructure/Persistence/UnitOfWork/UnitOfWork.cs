public sealed class UnitOfWork(AppDbContext dbContext) : IUnitOfWork
{
    public async Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken ct = default)
    {
        var transaction = await dbContext.Database.BeginTransactionAsync(ct);

        return new EfUnitOfWorkTransaction(transaction);
    }

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return dbContext.SaveChangesAsync(ct);
    }
}