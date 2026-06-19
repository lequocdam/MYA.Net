[ApiController]
[Route("api/addresses")]
public class AddressController(
    IAddressService addressService) : ControllerBase
{
    private Guid userId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> GetAll(
        CancellationToken ct)
    {
        var result = await addressService.GetAllAsync(
            userId,
            ct);

        return Ok(new ApiResponse<AddressDto>
        {
            Message = "",
            Data = result,
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        CreateAddressDto dto,
        CancellationToken ct)
    {
        var result = await addressService.CreateAsync(
            userId,
            dto,
            ct);

        return Ok(new ApiResponse<AddressDto>
        {
            Message = "",
            Data = result,
        });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        UpdateAddressDto dto,
        CancellationToken ct)
    {
        var userId = User.GetUserId();

        var result = await addressService.UpdateAsync(
            id,
            dto,
            userId,
            ct);

        return Ok(new ApiResponse<AddressDto>
        {
            Message = "",
            Data = result,
        });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken ct)
    {
        await addressService.DeleteAsync(
            id,
            userId,
            ct);

        return NoContent();
    }
}