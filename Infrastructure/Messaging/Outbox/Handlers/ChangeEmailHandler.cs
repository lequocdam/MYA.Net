public class ChangeEmailHandler(
    IOtpService otpService,
    IEmailService emailService,
    IRedisService redisService) : IOutboxMessageHandler
{
    public async Task HandleAsync(OutboxMessage message, CancellationToken ct)
    {
        var exists = await inboxRepository.ExistsAsync(
            message.Id,
            handlerName,
            ct);

        if (exists)
        {
            logger.LogInformation("OTP message {Id} skipped", message.Id);
            return;
        }

        var @event = JsonSerializer.Deserialize<EmailChangedEvent>(message.Payload)
            ?? throw new InvalidOperationException("...");

        var otp = await otpService.AddAsync(@event, ct);

        await emailService.SendAsync(otp, ct);

        await inboxRepository.InsertAsync(
            new InboxMessage
            {
                Id = Guid.NewGuid(),
                MessageId = message.Id,
                Type = message.Type,
                ProcessedAt =
                    DateTime.UtcNow
            },
            ct);



        await unitOfWork.SaveChangesAsync(ct);
    }
}