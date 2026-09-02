public class RegistrationCreatedHandler(
    IInboxRepository inboxRepository,
    IOtpService otpService,
    IEmailService emailService,
    ILogger<RegistrationCreatedOutboxHandler> logger) : IOutboxHandler
{
    public async Task HandleAsync(OutboxMessage message, CancellationToken ct)
    {
        var @event = JsonSerializer.Deserialize<RegistrationCreatedEvent>(message.Payload)
            ?? throw new InvalidOperationException("Invalid message payload");

        var dada = await otpService.SaveAsync(@event, ct);

        await emailService.SendAsync(dada.Target, dada.Code, ct);

        logger.LogInformation("Registration OTP sent. RegistrationId={RegistrationId}, MessageId={MessageId}",
            @event.RegistrationId,
            message.Id);
    }
}