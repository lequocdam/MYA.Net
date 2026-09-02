public record RegistrationCreatedEvent
{
    public Guid RegistrationId { get; init; }
    public int Version { get; init; } = 1;
    public string? Email { get; init; }
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}