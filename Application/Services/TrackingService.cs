public class HistoryService(
    IHistoryRepository historyRepository) : IHistoryService
{
    public async Task<List<TrackingDto>> GetByOrderIdAsync(
        Guid orderId,
        CancellationToken ct)
    {
        return await trackingRepository.Query()
            .Where(t => t.OrderId == orderId)
            .OrderBy(t => t.Date)
            .Select(t => new TrackingDto(
                t.Id,
                t.Date,
                t.Status,
                t.Message))
            .ToListAsync(ct);
    }
}