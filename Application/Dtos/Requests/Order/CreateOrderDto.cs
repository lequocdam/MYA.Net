public class CreatedOrderDto
{
    public Address Sender { get; set; }

    public Address Receiver { get; set; }

    public Guid ServiceId { get; set; }

    public List<Item> Items { get; set; }
}
