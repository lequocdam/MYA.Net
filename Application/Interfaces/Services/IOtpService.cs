public interface IOtpService
{
    Task VerifyAsync(Guid id, string code, CancellationToken ct);
}