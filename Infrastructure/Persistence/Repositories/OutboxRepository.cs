public sealed class OutboxRepository(AppDbContext context) : RepositoryBase<OutboxMessage>, IOutboxRepository
{
    public async Task AddAsync(OutboxMessage message, CancellationToken ct = default)
    {
        await context.OutboxMessages.AddAsync(user, ct);
    }

    public async Task ClaimAsync(OutboxMessage message, CancellationToken ct = default)
    {
        await context.OutboxMessages
            .Where(m => 
                m.Status == OutboxMessageStatus.Pending ||
                (m.Status == OutboxMessageStatus.Processing && m.LockedUntil < now))
            .OrderBy(m => m.CreatedAt)
            .Take(batchSize)
            .ToListAsync(ct);
    }
}