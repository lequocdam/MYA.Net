using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MYA.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/orders")]
[Authorize]
public class OrderController(
    IOrderService orderService,
    ICurrentUser currentUser) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Policies.OrderRead)]
    [ProducesResponseType(typeof(ApiResponse<Page<OrderRes>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<Page<OrderRes>>>> GetAll(
        [FromQuery] OrderFilterReq filter,
        CancellationToken ct)
    {
        var result = await orderService.GetAllAsync(filter, currentUser, ct);

        return Ok(new ApiResponse<Page<OrderRes>>
        {
            Data = result,
            Message = "Success"
        });
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Policies.OrderRead)]
    [ProducesResponseType(typeof(ApiResponse<OrderDetailRes>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<OrderDetailDto>>> GetById(
        Guid id,
        CancellationToken ct)
    {
        var result = await orderService.GetByIdAsync(id, currentUser, ct);

        return Ok(new ApiResponse<OrderDetailRes>
        {
            Data = result,
            Message = "Success"
        });
    }

    [HttpPost]
    [Authorize(Policy = Policies.OrderCreate)]
    [ProducesResponseType(typeof(ApiResponse<OrderRes>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<OrderRes>>> Create(
        [FromBody] CreateOrderReq req,
        CancellationToken ct)
    {
        var result = await orderService.CreateAsync(req, currentUser, ct);

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Id, version = "1.0" },
            new ApiResponse<OrderRes>
            {
                Data = result,
                Message = "Order created successfully"
            });
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Policies.OrderUpdate)]
    [ProducesResponseType(typeof(ApiResponse<OrderRes>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<OrderRes>>> Update(
        Guid id,
        [FromBody] UpdateOrderReq req,
        CancellationToken ct)
    {
        var result = await orderService.UpdateAsync(id, req, currentUser, ct);

        return Ok(new ApiResponse<OrderRes>
        {
            Data = result,
            Message = "Order updated successfully"
        });
    }

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Policy = Policies.OrderCancel)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Cancel(
        Guid id,
        [FromBody] CancelOrderDto dto,
        CancellationToken ct)
    {
        await orderService.CancelAsync(
            id,
            dto,
            currentUser,
            ct);

        return NoContent();
    }

    [HttpPost("{id:guid}/transition")]
    [Authorize(Policy = Policies.OrderTransition)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Transition(
        Guid id,
        [FromBody] TransitionOrderDto dto,
        CancellationToken ct)
    {
        await orderService.TransitionAsync(
            id,
            dto,
            currentUser,
            ct);

        return NoContent();
    }

    [HttpPost("estimate")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<EstimateDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<EstimateDto>>> Estimate(
        [FromBody] EstimateRequestDto dto,
        CancellationToken ct)
    {
        var result = await orderService.EstimateAsync(dto, ct);

        return Ok(new ApiResponse<EstimateDto>
        {
            Data = result,
            Message = "Success"
        });
    }

    [HttpPost("import")]
    [Authorize(Policy = Policies.OrderImport)]
    [ProducesResponseType(typeof(ApiResponse<OrderImportResultDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<OrderImportResultDto>>> Import(
        IFormFile file,
        CancellationToken ct)
    {
        var result = await orderService.ImportAsync(
            file,
            currentUser,
            ct);

        return Ok(new ApiResponse<OrderImportResultDto>
        {
            Data = result,
            Message = "Import completed"
        });
    }
}