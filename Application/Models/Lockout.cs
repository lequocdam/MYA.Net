public sealed class Lockout
{
    public bool IsLocked { get; init; },
    public DateTime LockedUntil { get; init; },
    public int FailedAttempts { get; init; },
}
