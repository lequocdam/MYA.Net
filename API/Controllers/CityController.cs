[ApiController]
[Route("api/cities")]
public class CityController(
    ICityService cityService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        CancellationToken ct)
    {
        var result = await cityService.GetAllAsync(ct);

        return Ok(result);
    }
}