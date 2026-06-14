public class Address
{
    public string Name { get; set; }
    public string Phone { get; set; }
    public string Mail { get; set; }
    public string Address { get; set; }
    public double Latitude  { get; set; }
    public double Longitude { get; set; }
}

// ─────────────────────────────────────────────
// ENTITY
// ─────────────────────────────────────────────

public class Address
{
    public Guid   Id        { get; set; }
    public string Name      { get; set; }
    public string Phone     { get; set; }
    public string City      { get; set; }
    public string Ward      { get; set; }
    public string Street    { get; set; }
    public double Latitude  { get; set; }
    public double Longitude { get; set; }
    public bool   IsDefault { get; set; }
    public Guid   UserId    { get; set; }
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
