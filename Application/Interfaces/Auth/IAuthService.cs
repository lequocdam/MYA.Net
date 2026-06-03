public interface IAuthService
{
    Task<string> RegisterAsync(RegisterDTO Dto);
    Task<UserDTO> VerifyAsync(OtpDTO Dto);
    Task<TokensDTO> LoginAsync(LoginDTO Dto, string Ip);
    Task<TokensDTO> RefreshAsync(RefreshDTO Dto);
    Task LogoutAsync(RefreshDTO Dto, string? Jti);
}