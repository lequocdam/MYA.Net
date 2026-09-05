[ApiController]
[Route("api/authentication")]
public class AuthenticationController(IAuthenticationService authenticationService) : ControllerBase
{
    private const string RefreshCookieName = "refreshToken";
    private const string CsrfCookieName = "csrfToken";
    private const string AuthCookiePath = "/api/auth";

    [HttpPost("register")]
    [EnableRateLimiting("auth-ip")]
    public async Task<ActionResult<ApiResponse<RegisterResponse>>> Register(
        [FromBody] RegisterRequest request,
        CancellationToken ct)
    {
        var response = await authenticationService.RegisterAsync(request, ct);

        return StatusCode(
            StatusCodes.Status201Created,
            new ApiResponse<RegisterResponse>
            {
                Message = "Registration created successfully.",
                Data = response
            });
    }

    [HttpPost("confirm")]
    [EnableRateLimiting("auth-ip")]
    public async Task<ActionResult<ApiResponse<ConfirmResponse>>> ConfirmAsync(
        [FromBody] ConfirmRequest request,
        CancellationToken ct)
    {
        var data = await authenticationService.ConfirmAsync(request, ct);

        SetRefreshTokenCookie(data.RefreshToken, data.RefreshExpiresAt);

        var response = new ConfirmResponse
        {
            AccessToken = data.AccessToken, 
            AccessExpiresAt = data.AccessExpiresAt
        }

        return StatusCode(
            StatusCodes.Status201Created,
            new ApiResponse<ConfirmResponse>
            {
                Message = "Account created successfully.",
                Data = response
            });
    }

    [HttpPost("resend")]
    [EnableRateLimiting("auth-ip")]
    public async Task<IActionResult> Resend(
        [FromBody] ResendRequest request,
        CancellationToken ct)
    {
        await authenticationService.ResendAsync(request, ct);

        return NoContent();
    }

    [HttpPost("forgot-password")]
    [EnableRateLimiting("forgot-password")]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordRequest request,
        CancellationToken ct)
    {
        var data = await authenticationService.ForgotPasswordAsync(request, ct);

        return NoContent();
    }

    [HttpPost("confirm-forgotpasword")]
    [EnableRateLimiting("confirm-forgotpasword")]
    public async Task<ActionResult> ConfirmForgotPassword(
        [FromBody] ForgotPasswordConfirmRequest request,
        CancellationToken ct)
    {
        await authenticationService.ForgotPasswordConfirmAsync(request, ct);

        return NoContent();
    }

    [HttpPost("login")]
    [EnableRateLimiting("login")]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var data = await authenticationService.LoginAsync(request, ct);

        SetAuthCookies(result.RefreshToken, result.RefreshExpiresAt);

        var response = new LoginResponse(data.AccessToken, data.AccessExpiresAt);

        return Ok(new ApiResponse<LoginResponse>
        {
            Message = "Account is logined successful.",
            Data = response,
        });
    }

    [Authorize]
    [HttpPost("refresh")]
    [EnableRateLimiting("refresh")]
    public async Task<IActionResult> Refresh([FromHeader(Name = "X-Refresh-Token")] RefreshDTO Dto)
    {
        var data = await authService.RefreshAsync(Dto, UserId);

        SetRefreshTokenCookie(data.RefreshToken, data.RefreshExpiresAt);

        var response = new RefreshResponse
        {
            AccessToken = data.AccessToken, 
            AccessExpiresAt = data.AccessExpiresAt
        };

        return Ok(new ApiResponse<RefreshResponse>
        {
            Message = "Account refreshed",
            Data = response,
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

    [HttpPost("change-email")]
    public async Task<IActionResult> ChangeEmail(
        ChangeEmailRequest request,
        CancellationToken ct)
    {
        await authenticationService.ChangeEmailAsync(request, ct);

        return Ok(new ApiResponse<object>
        {
            Message = "Email changed successfully.",
        });
    }

    private void SetAuthCookies(string refreshToken, DateTime refreshExpiresAt)
    {
        Response.Cookies.Append(RefreshCookieName, refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = refreshExpiresAt,
            Path = AuthCookiePath,
            IsEssential = true
        });

        var csrfToken = Guid.NewGuid().ToString("N");

        Response.Cookies.Append(CsrfCookieName, csrfToken, new CookieOptions
        {
            HttpOnly = false,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = refreshExpiresAt,
            Path = "/",
            IsEssential = true
        });
    }

    private void ClearAuthCookies()
    {
        Response.Cookies.Delete(RefreshCookieName, new CookieOptions { Path = AuthCookiePath });
        Response.Cookies.Delete(CsrfCookieName, new CookieOptions { Path = "/" });
    }

    private bool IsCsrfValid()
    {
        var cookieValue = Request.Cookies[CsrfCookieName];
        var headerValue = Request.Headers["X-CSRF-Token"].ToString();
        return !string.IsNullOrEmpty(cookieValue) && cookieValue == headerValue;
    }
}