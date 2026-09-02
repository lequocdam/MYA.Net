namespace Domain.Authentication.Registrations.Events;

public record RegistrationCreatedEvent
{
    public Guid RegistrationId { get; init; }
    public string Target { get; init; }
    public string Channel { get; init; }
    public string Code { get; init; };
    public string Purpose { get; init; };
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime ExpiredAt { get; init; }
}