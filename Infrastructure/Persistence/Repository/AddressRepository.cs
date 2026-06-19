public class AddressRepository : IAddressRepository
{
    private readonly AppDbContext _db;

    public AddressRepository(AppDbContext db)
    {
        _db = db;
    }

    public IQueryable<Address> Query()
    {
        return _db.Addresses;
    }

    public async Task AddAsync(
        Address address,
        CancellationToken ct)
    {
        await _db.Addresses.AddAsync(address, ct);
    }

    public async Task SaveChangesAsync(
        CancellationToken ct)
    {
        await _db.SaveChangesAsync(ct);
    }
}