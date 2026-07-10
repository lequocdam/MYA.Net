[ApiController]
[Route("api/addresses")]
public class AddressController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var result = await mediator.Send(
            new GetAllAddressesQuery(User.GetUserId()), ct);

        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        CreateAddressDto dto,
        CancellationToken ct)
    {
        var result = await mediator.Send(
            new CreateCommand(dto), ct);

        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        UpdateRequest request,
        CancellationToken ct)
    {
        var command = mapper.Map<UpdateCommand>(request)
            with { Id = id };

        var result = await mediator.Send(command, ct);

        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken ct)
    {
        await mediator.Send(
            new DeleteAddressCommand(
                id,
                User.GetUserId()), ct);

        return NoContent();
    }
}