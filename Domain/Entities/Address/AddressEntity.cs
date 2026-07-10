public class AddressEntity
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid CityId { get; private set; }
    public Guid WardId { get; private set; }
    public string Street { get; private set; }
    public double Latitude { get; private set; }
    public double Longitude { get; private set; }
    public bool IsDefault { get; private set; }
    public bool IsActive { get; private set; }
    public string Name { get; private set; }
    public string Phone { get; private set; }

    public static AddressEntity Create(
        CurrentUser currentUser,
        Address address,
        Contact contact)
    {
        return new AddressEntity
        {
            Id = Guid.NewGuid(),
            UserId = currentUser.UserId,
            CityId = address.CityId,
            WardId = address.WardId,
            Street = address.Street,
            Latitude = address.Latitude,
            longitude = address.Longitude,
            IsDefault = address.IsDefault,
            IsActive = true,
            Name = contact.Name,
            Phone = contact.Phone
        };
    }

    public void Update(
        Address address,
        Contact contact)
    {
        CityId = address.CityId;
        WardId = address.WardId;
        Street = address.Street;
        Latitude = address.Latitude;
        Longitude = address.Longitude;
        IsDefault = address.IsDefault;

        Name = contact.Name;
        Phone = contact.Phone;
    }
}



// ─────────────────────────────────────────────
// DTOs
// ─────────────────────────────────────────────

public record AddressDto(
    Guid    Id,
    string  Name,
    string  Phone,
    string? Email,
    string  Province,
    string  District,
    string  Ward,
    string  Street,
    double  Latitude,
    double  Longitude,
    bool    IsDefault
);

public record CreateAddressDto(
    string  Name,
    string  Phone,
    string? Email,
    string  Province,
    string  District,
    string  Ward,
    string  Street,
    double  Latitude,
    double  Longitude,
    bool    IsDefault
);

public record UpdateAddressDto(
    string  Name,
    string  Phone,
    string? Email,
    string  Province,
    string  District,
    string  Ward,
    string  Street,
    double  Latitude,
    double  Longitude,
    bool    IsDefault
);

// ─────────────────────────────────────────────
// CONTROLLER
// ─────────────────────────────────────────────

[ApiController]
[Route("api/addresses")]
[Authorize]
public class AddressController(IAddressService addressService) : ControllerBase
{
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct) =>
        Ok(await addressService.GetAllAsync(UserId, ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct) =>
        Ok(await addressService.GetByIdAsync(id, UserId, ct));

    [HttpPost]
    public async Task<IActionResult> Create(
        CreateAddressDto dto,
        CancellationToken ct) =>
        Ok(await addressService.CreateAsync(dto,
}
