[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService authService) : ControllerBase
{
    private Guid UserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private const string Ip = HttpContext.Connection.RemoteIpAddress?.ToString();

    [HttpPost("register")]
    public async Task<IActionResult> Register(
        [FromBody] RegisterDTO dto,
        [FromServices] IValidator<RegisterDTO> validator)
    {
        var result = await validator.ValidateAsync(dto);
        if (!result.IsValid)
            return BadRequest(result.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage }));

        var email = await authService.RegisterAsync(dto);
        return Ok(new ApiResponse<string>
        {
            Message = "Otp sent. Please check email",
            Data = email,
        });
    }

    [HttpPost("verify")]
    public async Task<IActionResult> Verify(
        [FromBody] OtpDTO dto,
        [FromServices] IValidator<OtpDTO> validator)
    {
        var result = await validator.ValidateAsync(dto);
        if (!result.IsValid)
            return BadRequest(result.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage }));

        var acc = await authService.VerifyAsync(dto);
        return Created(new ApiResponse<UserDTO>
        {
            Message = "Account created",
            Data = acc,
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginDTO Dto,
        [FromServices] IValidator<LoginDTO> validator)
    {
        var result = await validator.ValidateAsync(Dto);
        if (!result.IsValid)
            return BadRequest(result.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage }));

        var tokens = await authService.LoginAsync(Dto, Ip);

        return Ok(new ApiResponse<TokensDTO>
        {
            Message = "Account logined",
            Data = tokens,
        });
    }

    [Authorize]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(
        [FromHeader(Name = "X-Refresh-Token")] RefreshDTO Dto,
        [FromServices] IValidator<RefreshDTO> validator)
    {
        var result = await validator.ValidateAsync(Dto);
        if (!result.IsValid)
            return BadRequest(result.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage }));

        var tokens = await authService.RefreshAsync(Dto, UserId);

        return Ok(new ApiResponse<TokensDTO>
        {
            Message = "Account refreshed",
            Data = tokens,
        });
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(
        [FromHeader(Name = "X-Refresh-Token")] RefreshDTO Dto,
        [FromServices] IValidator<RefreshDTO> validator)
    {
        var result = await validator.ValidateAsync(Dto);
        if (!result.IsValid)
            return BadRequest(result.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage }));

        var Jti = User.FindFirst("Jti")?.Value;
        await authService.LogoutAsync(Dto, Jti);

        return NoContent();
    }

    [Authorize]
    [HttpPost("logout-all")]
    public async Task<IActionResult> LogoutAll(string reason, string? Jti)
    {
        var jti = User.FindFirst("jti")?.Value;
        await authService.LogoutAllAsync(UserId, reason, jti);

        return NoContent();
    }
}