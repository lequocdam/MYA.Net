public sealed class Session
{
    private Session()
    {
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public string? Ip { get; private set; }

    public string? Agent { get; private set; }

    public string? Device { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime? RevokedAt { get; private set; }

    public User User { get; private set; } = null!;

    public ICollection<RefreshToken> RefreshTokens { get; private set; }= new List<RefreshToken>();

    public static Session Create(
        Guid userId,
        string? ip,
        string? agent,
        string? device)
    {
        return new Session
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Device = device,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Revoke()
    {
        if (RevokedAt is null)
            RevokedAt = DateTime.UtcNow;
    }
}