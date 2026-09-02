using System.Collections.Frozen; 

public sealed class OutboxWorker(
    IEnumerable<IOutboxHandler> handlers,
    IOutboxRepository outboxRepository,
    IUnitOfWork unitOfWork,
    ILogger<OutboxWorker> logger) : IOutboxWorker
{
    public async Task<int> BatchAsync(int size, string workerId, CancellationToken ct)
    {
        var messages = await outboxRepository.ClaimAsync(size, workerId, ct);

        var successCount = 0;

        foreach (var message in messages)
        {
            message.Claim(workerId, TimeSpan.FromMinutes(5));

            var success = await ProcessAsync(message, workerId, ct);
            if (success) successCount++;

            await unitOfWork.SaveChangesAsync(ct);
        }

        logger.LogInformation($"Outbox batched {Success}/{Total} by {workerId} successfully");

        return successCount;
    }

    private async Task<bool> ProcessAsync(OutboxMessage message, string workerId, CancellationToken ct)
    {
        try
        {
            var handlerMap = handlers.ToFrozenDictionary(x => x.MessageType);

            if (!handlerMap.TryGetValue(message.Type, out var handler))
            {
                throw new InvalidOperationException($"No handler for {message.Type}");
            }

            await handler.HandleAsync(message, ct);

            message.MarkProcessed();

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "OutboxMessage {Id} of {Type} failed on attempt {RetryCount} (worker: {WorkerId})",
                message.Id, message.Type, message.RetryCount + 1, workerId);

            try
            {
                message.RecordFailure(
                    errorCode: exceptionClassifier.GetCode(ex),
                    error: ex.Message);

                await unitOfWork.SaveChangesAsync(ct);
            }

            return false;
        }
    }
}