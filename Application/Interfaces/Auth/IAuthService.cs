public interface IAuthService
{
    Task<string> RegisterAsync(RegisterDTO dto);
    Task<UserDTO> VerifyOTPAsync(OTPDTO dto);
    Task<TokensDTO> LoginAsync(LoginDTO Dto, string Ip);
    Task<TokensDTO> RefreshAsync(RefreshDTO Dto);
    Task LogoutAsync(RefreshDTO Dto, string? Jti);
}