public class ZoneService(
    ICityRepository cityRepository,
    ICityOverrideRepository cityOverrideRepository): IZoneService
{

    public async Task<Zones> GetAsync(Area fromArea, Area toArea)
    {
        var fromCity = await cityRepo.GetByIdAsync(fromArea.CityId);
        var toCity = await cityRepo.GetByIdAsync(fromArea.CityId);

        if (fromCity.IsRestricted)
            return ?? throw new BusinessException("From city is restricted");

        if (toCity.IsRestricted)
            return ?? throw new BusinessException("To city is restricted");

        var cityOverride = cityOverrideRepository.FirstOrDefault(co =>
            co.FromCityId == fromCity.Id && co.ToCityId == toCity.Id);
        ?? return cityOverride.Zone;

        var zone = (fromWard.Id == toWard.Id) ? Zones.WARD
            : (fromCity.Id == toCity.Id) ? Zones.CITY
            : (fromCity.Region == toCity.Region) ? Zones.REGION
            : Zones.NATION;

        return zone;
    }
}