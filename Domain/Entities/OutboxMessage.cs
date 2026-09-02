public sealed class OutboxMessage
{
    public Guid Id { get; private set; }
    public OutboxMessageType Type { get; private set; }
    public string? Payload { get; private set; }
    public OutboxMessageStatus Status { get; private set; }
    public string? Error { get; private set; }
    public int RetryCount { get; private set; }
    public DateTime OccurredAt { get; private set; }
    public DateTime NextRetryAt { get; private set; }

    public int MaxRetries { get; private set; } = 5;

    private OutboxMessage(){}

    public static OutboxMessage Create(
        string type, 
        string payload, 
        string? traceId = null, 
        int maxRetries = 5)
    {
        return new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = type,
            Payload = payload,
            Status = OutboxMessageStatus.Pending,
            RetryCount = 0,
            OccurredAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Lock message cho một Worker cụ thể
    /// </summary>
    public void MarkAsProcessing(string workerId)
    {
        Status = OutboxStatus.Processing;
        ProcessedBy = workerId;
    }

    public void MarkAsCompleted()
    {
        Status = OutboxStatus.Completed;
        ProcessedAt = DateTime.UtcNow;
        LastError = null;
    }

    public void RecordFailure(Exception exception)
    {
        RetryCount++;
        LastError = $"{exception.GetType().Name}: {exception.Message}\n{exception.StackTrace}";

        if (RetryCount >= MaxRetries)
        {
            Status = OutboxStatus.Failed;
            NextRetryAt = null;
        }
        else
        {
            Status = OutboxStatus.Pending;
            ProcessedBy = null;

            var delaySeconds = Math.Min((int)Math.Pow(2, RetryCount) * 5, 3600);
            NextRetryAt = DateTime.UtcNow.AddSeconds(delaySeconds);
        }
    }
}