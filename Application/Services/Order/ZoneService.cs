public class ZoneService : IZoneService
{
    public Zone Get(Address sender, Address receiver)
    {
        if (sender.Province == receiver.Province)
            return Zone.Local;

        if (sender.Region == receiver.Region)
            return Zone.SameRegion;

        return Zone.CrossRegion;
    }
}