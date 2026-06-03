[ApiController]
[Route("api/customer/orders")]
[Authorize]
public class OrderController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrderController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    private Guid userId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // GET /api/orders?status=Pending&page=1&pageSize=20
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] FilterDTO filter)
    {
        var result = await _orderService.GetAll(filter, userId);
        return Ok(result);
    }

    // GET /api/orders/{id}
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var order = await _orderService.GetById(id, CurrentUserId);
        return Ok(order);
    }

    // POST /api/orders
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrderDTO dto)
    {
        var order = await _orderService.Create(dto, CurrentUserId);
        return CreatedAtAction(nameof(GetById), new { id = order.Id }, new { order.Id, order.Code });
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