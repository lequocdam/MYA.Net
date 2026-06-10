[ApiController]
[Route("api/orders")]
[Authorize]
public class OrderController(
    IOrderService orderService;
) : ControllerBase
{
    private Guid userId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    [Authorize(Roles = "User, Admin")]
    public async Task<IActionResult> GetAll([FromQuery] OrderFilterDTO filter)
    {
        var result = await _orderService.GetList(filter, CurrentUserId);
        return Ok(result);
    }

    // GET /api/orders/{id}
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var order = await _orderService.GetById(id, CurrentUserId);
        return Ok(order);
    }

    [HttpPost]
    [Authorize(Roles = "User, Admin")]
    public async Task<IActionResult> Post(
        [FromBody] CreateOrderDto dto,
        [FromServices] IValidator<CreateOrderDto> validator,
        CancellationToken ct)
    {
        var result = await orderService.CreateAsync(dto, userId, ct);

        return Ok(new ApiResponse<OrderDto>
        {
            Message = "Đã tạo đơn hàng",
            Data    = result,
        });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Put(
        UpdateOrderDto dto,
        Guid id,
        CancellationToken ct)
    {
        await mediator.Send(new UpdateOrderCommand(dto, id, userId),
            ct);

        return NoContent();
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