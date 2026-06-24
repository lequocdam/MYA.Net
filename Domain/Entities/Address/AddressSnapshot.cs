public class AddressSnapshot
{
    public Guid Id { get; set; };
    public string Name { get; set; };
    public string Phone { get; set; };
    public string Street { get; set; };
    public Guid Ward { get; set; };
    public Guid City { get; set; };
    public Guid OrderId { get; set; };
}