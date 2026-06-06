[ApiController]
[Route("api/users")]
[Authorize]
public class UserController(IUserService userService) : ControllerBase
{
    private Guid userId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> All(
        [FromQuery] UserFilterDTO filter, CancellationToken ct)
    {
        var pageUsers = await userService.AllAsync(filter, ct);

        return Ok(new ApiResponse<Page<UserDto>>
        {
            Message = "",
            Data    = pageUsers,
        });
    }

    [HttpGet("profile")]
    public async Task<IActionResult> Profile(CancellationToken ct)
    {
        var user = await userService.ProfileAsync(userId, ct);

        return Ok(new ApiResponse<UserDto>
        {
            Message = "",
            Data    = user,
        });
    }
    
    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ById(Guide id, CancellationToken ct)
    {
        var user = await userService.ByIdAsync(id, ct);

        return Ok(new ApiResponse<UserDto>
        {
            Message = "",
            Data    = user,
        });
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(
        [FromBody] CreateUserDTO dto,
        [FromServices] IValidator<CreateUserDTO> validator,
        CancellationToken ct)
    {
        var validate = await validator.ValidateAsync(dto);
        if (!validate.IsValid)
            return BadRequest(validate.Errors.Select(e => new { property = e.PropertyName, message = e.ErrorMessage }));

        var user = await userService.CreateAsync(dto, ct);

        return CreatedAtAction(
            nameof(ById),
            new { id = user.Id },
            new ApiResponse<UserDTO>
            {
                Message = "Account created",
                Data = user
            });
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile(
        [FromBody] UpdateUserDTO Dto
        [FromServices] IValidator<UpdateUserDTO> validator,
        CancellationToken ct)
    {
        var validate = await validator.ValidateAsync(dto);
        if (!validate.IsValid)
            return BadRequest(validate.Errors.Select(e => new { property = e.PropertyName, message = e.ErrorMessage }));

        var user = await userService.UpdateAsync(dto, userId, ct);

        return Ok(new ApiResponse<UserDto>
        {
            Message = "Account updated",
            Data    = user,
        });
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateById(Guide id, CancellationToken ct)
    {
        var user = await userService.ByIdAsync(id, ct);

        return Ok(new ApiResponse<UserDto>
        {
            Message = "",
            Data    = user,
        });
    }

    [HttpPost("me/avatar")]
    public async Task<IActionResult> UploadAvatar(
        IFormFile file, CancellationToken ct)
    {
        var path = await userService.UploadAvatarAsync(file, CurrentUserId, ct);
        return Ok(new { Avatar = path });
    }

    [HttpPut("me/password")]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordDTO dto, CancellationToken ct)
    {
        await userService.ChangePasswordAsync(dto, CurrentUserId, ct);
        return NoContent();
    }

    // DELETE
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await userService.DeleteAsync(id, ct);
        return NoContent();
    }
}