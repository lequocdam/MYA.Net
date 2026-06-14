public interface IAddressService
{
    Task<List<AddressDto>> GetAllAsync(Guid userId, CancellationToken ct);
    Task<AddressDto>       GetByIdAsync(Guid id, Guid userId, CancellationToken ct);
    Task<AddressDto>       CreateAsync(CreateAddressDto dto, Guid userId, CancellationToken ct);
    Task                   UpdateAsync(Guid id, UpdateAddressDto dto, Guid userId, CancellationToken ct);
    Task                   DeleteAsync(Guid id, Guid userId, CancellationToken ct);
    Task                   SetDefaultAsync(Guid id, Guid userId, CancellationToken ct);
}
