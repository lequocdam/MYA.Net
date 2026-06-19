public class WardService(
    IWardRepository wardRepository) : IWardService
{
    public async Task<List<WardDto>> GetByCityIdAsync(
        Guid cityId,
        CancellationToken ct)
    {
        return await wardRepository.Query()
            .AsNoTracking()
            .Where(w => w.CityId == cityId)
            .OrderBy(w => w.Name)
            .Select(w => new WardDto(
                w.Id,
                w.Name,
                w.CityId))
            .ToListAsync(ct);
    }
}