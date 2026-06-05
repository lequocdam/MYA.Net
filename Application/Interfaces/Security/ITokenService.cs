public interface ITokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken(Guide userId);
    string HashToken(string token);
}