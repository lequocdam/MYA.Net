public class ZoneService(
    ICityRepository cityRepository,
    IWardRepository wardRepository,
    ICityOverrideRepository cityOverrideRepository): IZoneService
{

    public async Task<Zones> GetAsync(
        Address fromAddress, 
        Address toAddress)
    {
        var fromCityTask = await  cityRepository.FirstOrDefault(c => c.Id == fromAddress.CityId)
            ?? throw new NotFoundException("From city not found");

        var toCityTask = await cityRepository.FirstOrDefault(c => c.Id == toAddress.CityId)
            ?? throw new NotFoundException("To city not found");

        var fromWard = await wardRepository.FirstOrDefault(w => w.Id == fromAddress.WardId)
            ?? throw new NotFoundException("From ward not found");

        var toWard = await wardRepository.FirstOrDefault(w => w.Id == toAddress.WardId)
            ?? throw new NotFoundException("To ward not found");

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