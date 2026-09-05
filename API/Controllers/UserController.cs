[ApiController]
[Route("api/users")]
[Authorize]
public class UserController(IUserService userService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetList(
        [FromQuery] GetUsersRequest request,
        CancellationToken ct)
    {
        var result = await userService.GetUsersAsync(request, ct);

        return Ok(new ApiResponse<PagedResult<UserResponse>>
        {
            Message = "Users retrieved successfully.",
            Data = result
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken ct)
    {
        var result = await userService.GetByIdAsync(id, ct);

        return Ok(new ApiResponse<UserResponse>
        {
            Message = "User retrieved successfully.",
            Data = result
        });
    }

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile(CancellationToken ct)
    {
        var result = await userService.GetProfileAsync(userId, ct);

        return Ok(new ApiResponse<UserDto>
        {
            Message = "Account profile got",
            Data    = result,
        });
    }

    [HttpPost]
    [Authorize(Roles = "Admin, Manager")]
    [RequirePermission(UserPermissions.Create)]
    public async Task<IActionResult> Create(
        [FromBody] CreateRequest request,
        CancellationToken ct)
    {
        var result = await userService.CreateAsync(request, ct);

        return StatusCode(StatusCodes.Status201Created, new ApiResponse<UserResponse>
        {
            Message = "User created successfully.",
            Data = result
        });
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin, Manager")]
    [RequirePermission(UserPermissions.Update)]
    public async Task<IActionResult> Update(
        Guide id, 
        [FromBody] UpdateResponse id,
        CancellationToken ct)
    {
        var result = await userService.UpdateAsync(id, id, ct);

        return Ok(new ApiResponse<UpdateResponse>
        {
            Message = "Account updated successfully.",
            Data = result,
        });
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin, Manager")]
    [RequirePermission(UserPermissions.Delete)]
    public async Task<IActionResult> Delete(
        Guide id, 
        CancellationToken ct)
    {
        await userService.DeleteAsync(id, ct);

        return NoContent();
    }
}