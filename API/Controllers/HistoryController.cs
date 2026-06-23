[ApiController]
[Route("api/orders/{orderId:guid}/histories")]
public class HistoriesController(
    IHistoryService historyService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        Guid orderId,
        CancellationToken ct)
    {
        var result = await historyService.GetByOrderIdAsync(orderId, ct);

        return Ok(new ApiResponse<HistoryDto>
        {
            Message = "Histories got successfully",
            Data = result,
        });
    }
}