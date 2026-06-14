[ApiController]
[Route("api/warehouses")]
public class WarehouseController(IWarehouseService warehouseService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct) =>
        Ok(await warehouseService.GetAllAsync(ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct) =>
        Ok(await warehouseService.GetByIdAsync(id, ct));

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(
        CreateWarehouseDto dto,
        CancellationToken ct) =>
        Ok(await warehouseService.CreateAsync(dto, ct));

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(
        Guid id,
        UpdateWarehouseDto dto,
        CancellationToken ct)
    {
        await warehouseService.UpdateAsync(id, dto, ct);
        return NoContent();
    }

    [HttpGet("{id:guid}/coverages")]
    public async Task<IActionResult> GetCoverages(Guid id, CancellationToken ct) =>
        Ok(await warehouseService.GetCoveragesAsync(id, ct));

    [HttpPut("{id:guid}/coverages")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpsertCoverages(
        Guid id,
        UpsertCoverageDto dto,
        CancellationToken ct)
    {
        await warehouseService.UpsertCoveragesAsync(id, dto, ct);
        return NoContent();
    }
}