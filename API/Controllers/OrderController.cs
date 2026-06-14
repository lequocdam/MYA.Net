[ApiController]
[Route("api/orders")]
[Authorize]
public class OrderController(
    IOrderService orderService
) : ControllerBase
{
    private Guid userId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    [Authorize(Roles = "Admin, Manager, User")]
    public async Task<IActionResult> GetAll(
        [FromQuery] OrderFilterDto filter,
        CancellationToken ct)
    {
        var result = await orderService.GetAllAsync(filter, userId, ct);

        return Ok(new ApiResponse<Page<UserDto>>
        {
            Message = "",
            Data    = result,
        });
    }

    // GET /api/orders/{id}
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var order = await _orderService.GetById(id, CurrentUserId);
        return Ok(order);
    }

    [HttpPost]
    [Authorize(Roles = "Admin, Manager, User")]
    public async Task<IActionResult> Create(
        [FromBody] CreateOrderDto dto, 
        CancellationToken ct)
    {
        var result = await orderService.CreateAsync(dto, userId, ct);

        return Ok(new ApiResponse<UserDto>
        {
            Message = "Order created successfully",
            Data    = result,
        });
    }

    // DELETE /api/orders/{id}
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Cancel(Guid id, [FromBody] CancelOrderDTO dto)
    {
        await _orderService.Cancel(id, dto.Reason, CurrentUserId);
        return NoContent();
    }

    // PUT /api/orders/{id}/status
    [HttpPut("{id:guid}/status")]
    [Authorize(Roles = "Admin,Ops,Driver")]  // khách không được gọi endpoint này
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateStatusDTO dto)
    {
        await _orderService.UpdateStatus(id, dto.Trigger, CurrentUserId);
        return NoContent();
    }
}