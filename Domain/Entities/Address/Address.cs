public class Address
{
    private Guid Id { get; private set; }
    private Guid UserId { get; private set; }
    private string Name { get; private set; }
    private string Phone { get; private set; }
    private string Street { get; private set; }
    private Guid WardId { get; private set; }
    private Guid CityId { get; private set; }
    private double Latitude { get; private set; }
    private double Longitude { get; private set; }
    private bool IsDefault { get; private set; }
    private bool IsActive { get; private set; }

    public static Address Create(
        Guid userId,
        string name,
        string phone,
        string street,
        Guid wardId,
        Guid cityId,
        double latitude,
        double longitude,
        bool isDefault)
    {
        return new Address
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = name,
            Phone = phone,
            Street = street,
            WardId = wardId,
            CityId = cityId,
            Latitude = latitude,
            longitude = longitude,
            IsDefault = isDefault,
            IsActive = true
        };
    }

    public static void Update(
        string name,
        string phone,
        string street,
        Guid wardId,
        Guid cityId,
        double latitude,
        double longitude,
        bool isDefault)
    {
        return new Address
        {
            Name = name,
            Phone = phone,
            Street = street,
            WardId = wardId,
            CityId = cityId,
            Latitude = longitude,
            longitude = latitude,
            IsDefault = isDefault
        };
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
