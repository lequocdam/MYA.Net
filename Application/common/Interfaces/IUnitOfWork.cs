namespace Application.Common.Interfaces;

public interface IUnitOfWork
{
    Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken ct = default);

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}