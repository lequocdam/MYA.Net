public sealed class ConfirmRequest
{
    public Guid RegistrationId { get; init; }

    public string? Code { get; init; }
}
