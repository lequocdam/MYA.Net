public class HistoryService(
    IHistoryRepository historyRepository) : IHistoryService
{
    public async Task<List<HistoryDto>> GetByOrderIdAsync(
        Guid orderId,
        CancellationToken ct)
    {
        return await orderHistoryRepository.Query()
            .Where(h => h.OrderId == orderId)
            .OrderBy(h => h.Date)
            .Select(h => new HistoryDto(
                h.Id,
                h.Date,
                h.Status,
                h.Note,
                h.UserId))
            .ToListAsync(ct);
    }
}