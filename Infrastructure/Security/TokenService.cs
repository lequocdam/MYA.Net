public class TokenService(IConfiguration config) : ITokenService
{
    private readonly string _jwtKey = config["Jwt:Key"]
        ?? throw new InvalidOperationException("Jwt:Key is not configured");

    private readonly string _jwtIssuer = config["Jwt:Issuer"]
        ?? throw new InvalidOperationException("Jwt:Issuer is not configured");

    private readonly string _jwtAudience = config["Jwt:Audience"]
        ?? throw new InvalidOperationException("Jwt:Audience is not configured");

    public string GenerateAccessToken(User user)
    {
        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, user.Role),
        };

        var token = new JwtSecurityToken(
            issuer:             _jwtIssuer,
            audience:           _jwtAudience,
            claims:             claims,
            expires:            DateTime.UtcNow.AddMinutes(15),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var bytes = new byte[64];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }

    public string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }
}