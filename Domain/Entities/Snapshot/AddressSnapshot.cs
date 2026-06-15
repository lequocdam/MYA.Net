public class AddressSnapshot
{
    public string Name { get; private set; } = default!;
    public string Phone { get; private set; } = default!;
    public string City { get; private set; } = default!;
    public string Ward { get; private set; } = default!;
    public string Street { get; private set; } = default!;
    public double Latitude { get; private set; }
    public double Longitude { get; private set; }

    public static AddressSnapshot From(Address address)
    {
        return new AddressSnapshot
        {
            Name = address.Name,
            Phone = address.Phone,
            City = address.City,
            Ward = address.Ward,
            Street = address.Street,
            Latitude = address.Latitude,
            Longitude = address.Longitude
        };
    }
}