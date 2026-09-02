public sealed class EfUnitOfWorkTransaction(IDbContextTransaction transaction) : IUnitOfWorkTransaction
{
    public Task CommitAsync(CancellationToken ct = default)
    {
        return transaction.CommitAsync(ct);
    }

    public Task RollbackAsync(CancellationToken ct = default)
    {
        return transaction.RollbackAsync(ct);
    }

    public ValueTask DisposeAsync()
    {
        return transaction.DisposeAsync();
    }
}