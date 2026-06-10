[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService authService) : ControllerBase
{
    private Guid UserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost("register")]
    [EnableRateLimiting("register")]
    public async Task<IActionResult> Register(
        [FromBody] RegisterDto dto,
        [FromServices] IValidator<RegisterDto> validator,
        CancellationToken ct)
    {
        var validate = await validator.ValidateAsync(dto);
        if (!validate.IsValid)
            return BadRequest(validate.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage }));

        await authService.RegisterAsync(dto, ct);
        return NoContent();
    }

    [HttpPost("resend")]
    [EnableRateLimiting("resend")]
    public async Task<IActionResult> ResendOtp(
        [FromBody] ResendOtpDto dto,
        [FromServices] IValidator<ResendOtpDto> validator,
        CancellationToken ct)
    {
        var validate = await validator.ValidateAsync(dto);
        if (!validate.IsValid)
            return BadRequest(validate.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage }));

        await authService.ResendOtpAsync(dto, ct);
        return NoContent();
    }

    [HttpPost("verify")]
    [EnableRateLimiting("verify")]
    public async Task<IActionResult> VerifyOtpAsync(
        [FromBody] OtpDto dto,
        [FromServices] IValidator<OtpDto> validator,
        CancellationToken ct)
    {
        var validate = await validator.ValidateAsync(dto);
        if (!validate.IsValid)
            return BadRequest(validate.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage }));

        var result = await authService.VerifyOtpAsync(dto, ct);

        return Ok(new ApiResponse<UserDto>
        {
            Message = "",
            Data = result,
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