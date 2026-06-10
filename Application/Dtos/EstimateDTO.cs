public class CreatingOrderDTO
{
    public Address Sender { get; set; }

    public Address Receiver { get; set; }

    public List<Item> Items { get; set; }
}
