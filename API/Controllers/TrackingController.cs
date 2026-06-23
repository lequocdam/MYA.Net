[ApiController]
[Route("api/orders/{orderId:guid}/trackings")]
public class TrackingController(
    ITrackingService trackingService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        Guid orderId,
        CancellationToken ct)
    {
        var result = await trackingService.GetByOrderIdAsync(orderId, ct);

        return Ok(new ApiResponse<TrackingDto>
        {
            Message = "Trackings got successfully",
            Data = result,
        });
    }
}