public class AddressService
{
    private readonly AppDbContext _context;

    public AddressService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<AddressDTO>> GetAll(int userId)
    {
        return await _context.Addresses
            .Where(a => a.UserId == userId)
            .Select(a => new AddressDTO
            {
                Name = a.Name,
                Phone = a.Phone,
                Email = a.Email,
                Address = a.Address,
                IsDefault = a.IsDefault
            })
            .ToListAsync();
    }

    public async Task<AddressDTO> Create(CreatedAddressDTO dto, int userId)
    {
        if (dto.IsDefault)
        {
            var oldDefaults = _context.Addresses
                .Where(x => x.IsDefault && x.UserId == userId);

            await oldDefaults.ForEachAsync(x => x.IsDefault = false);
        }

        var address = new Address
        {
            Name = dto.Name,
            Phone = dto.Phone,
            Email = dto.Email,
            Address = dto.Address,
            IsDefault = dto.IsDefault,
            UserId = userId
        };

        _context.Add(address);
        await _context.SaveChangesAsync();

        return new AddressDTO
        {
            Id = address.Id,
            Name = address.Name,
            Phone = address.Phone,
            Email = address.Email,
            Address = address.Address,
            IsDefault = address.IsDefault
        };
    }

    public async Task<AddressDTO> Update(AddressUpdateDto dto, int id, int userId)
    {
        var address = await _context.Addresses
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

        if (address == null) return null;

        if (dto.IsDefault)
        {
            var oldDefaults = _context.Addresses
                .Where(x => x.UserId == userId && x.IsDefault);

            await oldDefaults.ForEachAsync(x => x.IsDefault = false);
        }

        address.Name = dto.Name;
        address.Phone = dto.Phone;
        address.Email = dto.Email;
        address.Address = dto.Address;
        address.IsDefault = dto.IsDefault;

        await _context.SaveChangesAsync();

        return new AddressDTO
        {
            Id = address.Id,
            Name = address.Name,
            Phone = address.Phone,
            Email = address.Email,
            Address = address.Address,
            IsDefault = address.IsDefault
        };
    }

    public async Task<bool> Delete(int id, int userId)
    {
        var address = await _context.Addresses
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);

        if (address == null) return false;

        address.IsDeleted = true;

        await _context.SaveChangesAsync();
        return true;
    }
}