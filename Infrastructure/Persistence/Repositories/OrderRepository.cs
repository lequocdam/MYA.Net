public sealed class OrderRepository(AppDbContext db) : IOrderRepository
{
    public IQueryable<Order> Query() => db.Orders.AsQueryable();

    public async Task<Order?> FirstOrDefaultAsync(
        ISpecification<Order> spec, CancellationToken ct = default)
        => await ApplySpecification(spec).FirstOrDefaultAsync(ct);

    public async Task<List<Order>> ToListAsync(
        ISpecification<Order> spec,
        CancellationToken ct = default)
        => await ApplySpecification(spec).ToListAsync(ct);

    public async Task<int> CountAsync(
        ISpecification<Order> spec, CancellationToken ct = default)
        => await ApplySpecification(spec).CountAsync(ct);

    public async Task AddAsync(Order order, CancellationToken ct = default)
        => await db.Orders.AddAsync(order, ct);

    public void Update(Order order) => db.Orders.Update(order);

    public void Remove(Order order) => db.Orders.Remove(order);

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
        => await db.SaveChangesAsync(ct);

    public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct = default)
        => await db.Database.BeginTransactionAsync(ct);

    private IQueryable<Order> ApplySpecification(ISpecification<Order> spec)
        => SpecificationEvaluator.Default.GetQuery(db.Orders.AsQueryable(), spec);
}