public class Warehouse
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string Street { get; private set; }
    public Guid WardId { get; private set; }
    public Guid CityId { get; private set; }
    public double Latitude { get; private set; }
    public double Longitude { get; private set; }
    public bool IsActive { get; private set; }

    public static Warehouse Create(
        string name,
        string street,
        Guid wardId,
        Guid cityId,
        double latitude,
        double longitude)
    {
        return new Warehouse
        {
            Id = Guid.NewGuid(),
            Name = name,
            Street = street,
            WardId = wardId,
            CityId = cityId,
            Latitude = latitude,
            longitude = longitude,
            IsActive = true
        };
    }

    public static void Update(
        string name,
        string street,
        Guid wardId,
        Guid cityId,
        double latitude,
        double longitude)
    {
        return new Warehouse
        {
            Name = name,
            Street = street,
            WardId = wardId,
            CityId = cityId,
            Latitude = longitude,
            longitude = latitude,
        };
    }
}