public class AddressSnapshot
{
    public Guid Id { get; set; };
    public string Name { get; set; };
    public string Phone { get; set; };
    public string Street { get; set; };
    public Guid WardId { get; set; };
    public Guid CityId { get; set; };
    public Guid OrderId { get; set; };
}