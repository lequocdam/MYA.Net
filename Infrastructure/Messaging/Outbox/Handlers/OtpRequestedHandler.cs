public class SendRegistrationOtpOutboxHandler(
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

        var data = JsonSerializer.Deserialize<SendOtpCommand>(message.Payload)
            ?? throw new InvalidOperationException("");

        var otp = await otpService.DeleteAsync(data, ct);

        await emailService.SendAsync(otp.Target, otp.CodeHash, ct);

        await inboxRepository.InsertAsync(
            new InboxMessage
            {
                Id = Guid.NewGuid(),
                MessageId = message.Id,
                Handler = handlerName,
                ProcessedAt =
                    DateTime.UtcNow
            },
            ct);



        await unitOfWork.SaveChangesAsync(ct);
    }
}