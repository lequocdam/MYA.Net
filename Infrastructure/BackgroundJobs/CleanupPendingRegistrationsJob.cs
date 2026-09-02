public class CleanupPendingRegistrationsJob(
    IRegistrationRepository registrationRepository,
    IUnitOfWork unitOfWork,
    ILogger<CleanupPendingRegistrationsJob> logger)
{
    private static readonly TimeSpan PendingTtl = TimeSpan.FromMinutes(15);

    public async Task ExecuteAsync(CancellationToken ct)
    {
        var cutoff = DateTime.UtcNow.Subtract(PendingTtl);

        var deletedCount = await registrationRepository.DeleteByPendingAsync(cutoff, ct);

        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Cleanup job deleted {Count} pending registrations older than {Cutoff}", deletedCount, cutoff);
    }
}