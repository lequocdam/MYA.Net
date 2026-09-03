public record TokenData
{
    public string AccessToken { get; init; }
    public DateTime AccessExpiresAt { get; init; }
    public string RefreshToken { get; init; }
    public DateTime RefreshExpiresAt { get; init; }
}
