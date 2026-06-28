public class AddressSnapshot
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string Phone { get; private set; }
    public string Street { get; private set; }
    public Guid CityId { get; private set; }
    public Guid WardId { get; private set; }
    public Guid OrderId { get; private set; }

    public static AddressSnapshot Create(Guid orderId, Address address)
    {
        return new AddressSnapshot
        {
            Id = Guid.NewGuid(),
            Name = address.Name,
            Phone = address.Phone,
            Street = address.Street,
            CityId = address.CityId,
            WardId = address.WardId,
            OrderId = orderId,
        }
    }
}