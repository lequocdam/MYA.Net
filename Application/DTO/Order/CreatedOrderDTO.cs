public class CreatedOrderDTO
{
    public Address Sender { get; set; }

    public Address Receiver { get; set; }

    public string CategoryId { get; set; }

    public List<Item> Items { get; set; }
}
