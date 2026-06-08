[ApiController]
[Route("api/users")]
[Authorize]
public class UserController(IUserService userService) : ControllerBase
{
    private Guid userId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll(
        [FromQuery] UserFilterDTO filter, CancellationToken ct)
    {
        var pageUsers = await userService.AllAsync(filter, ct);

        return Ok(new ApiResponse<Page<UserDto>>
        {
            Message = "",
            Data    = pageUsers,
        });
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetDetail(Guid id, CancellationToken ct)
    {
        var result = await userService.GetDetailAsync(id, ct);

        return Ok(new ApiResponse<UserDto>
        {
            Message = "Account detail got",
            Data    = result,
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
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Post(
        [FromBody] CreateUserDto dto,
        [FromServices] IValidator<CreateUserDto> validator,
        CancellationToken ct)
    {
        var validate = await validator.ValidateAsync(dto);
        if (!validate.IsValid)
            return BadRequest(validate.Errors.Select(e => new { property = e.PropertyName, message = e.ErrorMessage }));

        var result = await userService.CreateAsync(dto, ct);

        return Ok(new ApiResponse<UserDto>
        {
            Message = "Account created",
            Data    = result,
        });
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Put(
        [FromBody] UpdateUserDto dto,
        Guide id, 
        CancellationToken ct)
    {
        var result = await userService.UpdateAsync(dto, id, ct);

        return Ok(new ApiResponse<UserDto>
        {
            Message = "Account updated",
            Data    = result,
        });
    }

    [HttpPut("{id:guid}/activate")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Activate(
        Guide id, 
        CancellationToken ct)
    {
        await userService.ActivateAsync(id, ct);
        return NoContent();
    }

    [HttpPut("profile")]
    public async Task<IActionResult> PutProfile(
        [FromBody] UpdateUserDto dto,
        CancellationToken ct)
    {
        var user = await userService.UpdateProfileAsync(dto, userId, ct);

        return Ok(new ApiResponse<UserDto>
        {
            Message = "Account profile updated",
            Data    = user,
        });
    }

    [HttpPost("profile/avatar")]
    public async Task<IActionResult> UploadAvatar(
        IFormFile file, CancellationToken ct)
    {
        var path = await userService.UploadAvatarAsync(file, CurrentUserId, ct);
        return Ok(new { Avatar = path });
    }

    [HttpPut("profile/password")]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordDto dto, 
        CancellationToken ct)
    {
        await userService.ChangePasswordAsync(dto, userId, ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await userService.DeleteAsync(id, ct);
        return NoContent();
    }
}