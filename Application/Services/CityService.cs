public class CityService(
    ICityRepository cityRepository) : ICityService
{
    public async Task<List<CityDto>> GetAllAsync(
        CancellationToken ct)
    {
        return await cityRepository.Query()
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CityDto(
                c.Id,
                c.Name))
            .ToListAsync(ct);
    }
}