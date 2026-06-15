public class ZoneService(
    ICityRepository cityRepository,
    ICityOverrideRepository cityOverrideRepository) : IZoneService
{

    public async Task<ZoneResult> GetByIdAsync(Guid FromCityCode, Guid ToCityCode )
    {
        var fromCity = cityRepository.FirstOrDefault(c => c.Code == fromAddress.Code)
            ?? throw new NotFoundException("From city not found");

        var toCity   = cityRepository.FirstOrDefault(c => c.Code == toAddress.Code)
            ?? throw new NotFoundException("To city not found");

        if (fromCity.IsRestricted)
            return ?? throw new NotFoundException("To city not found");

        if (toCity.IsRestricted)
            return ?? throw new NotFoundException("To city not found");

        var cityOverride = cityOverrideRepository.FirstOrDefault(co =>
            co.FromCityId == fromCity.Id && co.ToCityId == toCity.Id);
        ?? return cityOverride.Zone;

        var zone = (fromCity.Code   == toCity.Code) ? Zones.CITY
                 : (fromCity.Region == to.Region)   ? Zones.REGION
                 : Zones.NATION;

        return zone;
    }
}