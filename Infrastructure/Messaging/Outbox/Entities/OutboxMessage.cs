public sealed class OutboxMessage
{
    private const int MaxRetries = 5;

    public Guid Id { get; private set; }
    public string? WorkerId { get; private set; }
    public OutboxMessageType Type { get; private set; }
    public string Payload { get; private set; }
    public OutboxMessageStatus Status { get; private set; }
    public DateTime? LockedUntil { get; private set; }
    public DateTime OccurredAt { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    public DateTime? NextAttemptAt { get; private set; }
    public string? Error { get; private set; }
    public int RetryCount { get; private set; }
    public byte[]? RowVersion { get; private set; }

    private OutboxMessage()
    {
        Payload = string.Empty;
    }

    private OutboxMessage(OutboxMessageType type, string payload)
    {
        Id = Guid.NewGuid();
        Type = type;
        Payload = payload;
        Status = OutboxMessageStatus.Pending;
        OccurredAt = DateTime.UtcNow;
    }

    public static OutboxMessage Create(OutboxMessageType type, string payload)
    {
        if (!Enum.IsDefined(type))
            throw new DomainException("Invalid outbox message type.");

        if (string.IsNullOrWhiteSpace(payload))
            throw new DomainException("Outbox message payload cannot be empty.");

        return new OutboxMessage(type, payload);
    }

    public void MarkProcessing()
    {
        if (Status != OutboxMessageStatus.Pending)
            throw new InvalidOperationException("Only pending messages can be processed.");

        Status = OutboxMessageStatus.Processing;
    }

    public void MarkProcessed()
    {
        Status = OutboxMessageStatus.Processed;
        ProcessedAt = DateTime.UtcNow;
        Error = null;
        NextAttemptAt = null;
        LockedUntil = null;
    }

    // Gộp MarkFailed + RecordFailure thành 1 method duy nhất, luôn enforce dead-letter khi vượt MaxRetries
    public void RecordFailure(string error)
    {
        RetryCount++;
        Error = error;

        if (RetryCount >= MaxRetries)
        {
            Status = OutboxMessageStatus.DeadLetter;
            NextAttemptAt = null;
            return;
        }

        Status = OutboxMessageStatus.Pending;
        NextAttemptAt = CalculateNextAttempt();
    }

    // Chỉ dùng cho hành động thủ công (admin can thiệp), tách rõ khỏi flow tự động
    public void ForceDeadLetter(string reason)
    {
        Status = OutboxMessageStatus.DeadLetter;
        Error = reason;
        NextAttemptAt = null;
    }

    private DateTime CalculateNextAttempt()
    {
        // Exponential backoff: 2^RetryCount phút, tối đa 30 phút
        var delayMinutes = Math.Min(Math.Pow(2, RetryCount), 30);
        return DateTime.UtcNow.AddMinutes(delayMinutes);
    }
}