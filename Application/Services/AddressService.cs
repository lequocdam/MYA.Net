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
            .Where(a => a.UserId == userId && a.IsActive)
            .OrderByDescending(a => a.IsDefault)
            .Select(a => new AddressDto(
                a.Id,
                a.Name,
                a.Phone,
                a.Street,
                a.WardId,
                a.CityId,
                a.IsDefault))
            .ToListAsync(ct);
    }

    public async Task<AddressDto> CreateAsync(
        Guid userId,
        CreateAddressDto dto,
        CancellationToken ct)
    {
        if (dto.IsDefault)
            await ClearDefaultAsync(userId, ct);

        var address = Address.Create(
            userId,
            dto.Name,
            dto.Phone,
            dto.Street,
            dto.WardId,
            dto.CityId,
            dto.Latitude,
            dto.Longitude,
            dto.IsDefault  
        )

        await addressRepository.AddAsync(address, ct);
        await addressRepository.SaveChangesAsync(ct);

        return mapper.Map<AddressDto>(address);
    }

    public async Task<AddressDto> UpdateAsync(
        Guid id,
        UpdateAddressDto dto,
        Guid userId,
        CancellationToken ct)
    {
        var address = await addressRepository.Query()
            .FirstOrDefaultAsync(a => a.Id == id 
                    && a.UserId == userId
                    && a.IsActive, ct)
            ?? throw new NotFoundException("Address not found");

        if (dto.IsDefault && !address.IsDefault)
            await ClearDefaultAsync(userId, ct);

        address.Update(
            dto.Name,
            dto.Phone,
            dto.Street,
            dto.WardId,
            dto.CityId,
            dto.Latitude,
            dto.Longitude,
            dto.IsDefault  
        )

        await addressRepository.SaveChangesAsync(ct);

        return mapper.Map<AddressDto>(address);
    }

    public async Task DeleteAsync(
        Guid id,
        Guid userId,
        CancellationToken ct)
    {
        var address = await addressRepository.Query()
            .FirstOrDefaultAsync(a => a.Id == id 
                    && a.UserId == userId
                    && a.IsActive, ct)
            ?? throw new NotFoundException("Address not found");

        if (address.IsDefault)
            throw new BusinessException("Default address cannot be deleted");

        address.IsActive = false;

        await addressRepository.SaveChangesAsync(ct);
    }

    private async Task ClearDefaultAsync(Guid userId, CancellationToken ct)
    {
        await addressRepository.Query()
            .Where(a => a.UserId == userId 
                && a.IsDefault
                && a.IsActive)
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.IsDefault, false), ct);
    }
}