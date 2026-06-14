public class AddressService(
    IAddressRepository addressRepository,
    IMapper mapper,
    ILogger<AddressService> logger) : IAddressService
{
    public async Task<List<AddressDto>> GetAllAsync(
        Guid userId,
        CancellationToken ct)
    {
        return await addressRepository.Query()
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.IsDefault)
            .Select(a => new AddressDto(
                a.Id,
                a.Name,
                a.Phone,
                a.City,
                a.Ward,
                a.Street,
                a.IsDefault))
            .ToListAsync(ct);
    }

    public async Task<AddressDto> CreateAsync(
        CreateAddressDto dto,
        Guid userId,
        CancellationToken ct)
    {
        if (dto.IsDefault)
            await ClearDefaultAsync(userId, ct);

        var address = new Address
        {
            Id        = Guid.NewGuid(),
            Name      = dto.Name,
            Phone     = dto.Phone,
            City      = dto.City,
            Ward      = dto.Ward,
            Street    = dto.Street,
            Latitude  = dto.Latitude,
            Longitude = dto.Longitude,
            IsDefault = dto.IsDefault,
            UserId    = userId
        };

        await addressRepository.AddAsync(address, ct);
        await addressRepository.SaveChangesAsync(ct);

        return mapper.Map<AddressDto>(address);
    }

    public async Task UpdateAsync(
        Guid id,
        UpdateAddressDto dto,
        Guid userId,
        CancellationToken ct)
    {
        var address = await addressRepository.Query()
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId, ct)
            ?? throw new NotFoundException("Address not found");

        if (dto.IsDefault && !address.IsDefault)
            await ClearDefaultAsync(userId, ct);

        address.Name      = dto.Name;
        address.Phone     = dto.Phone;
        address.City      = dto.City;
        address.Ward      = dto.Ward;
        address.Street    = dto.Street;
        address.Latitude  = dto.Latitude;
        address.Longitude = dto.Longitude;
        address.IsDefault = dto.IsDefault;

        await addressRepository.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(
        Guid id,
        Guid userId,
        CancellationToken ct)
    {
        var address = await addressRepository.Query()
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId, ct)
            ?? throw new NotFoundException("Address not found");

        if (address.IsDefault)
            throw new BusinessException("Default address cannot be deleted");

        address.IsActive = false;

        await addressRepository.SaveChangesAsync(ct);
    }

    public async Task SetDefaultAsync(
        Guid id,
        Guid userId,
        CancellationToken ct)
    {
        var address = await addressRepository.Query()
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId, ct)
            ?? throw new NotFoundException("Address", id);

        if (address.IsDefault) return;

        await ClearDefaultAsync(userId, ct);

        address.IsDefault = true;
        await addressRepository.SaveChangesAsync(ct);
    }

    private async Task ClearDefaultAsync(Guid userId, CancellationToken ct)
    {
        await addressRepository.Query()
            .Where(a => a.UserId == userId && a.IsDefault)
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.IsDefault, false), ct);
    }
}