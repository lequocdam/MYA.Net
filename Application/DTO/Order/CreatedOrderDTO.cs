using System.ComponentModel.DataAnnotations;

public class CreatedOrderDTO
{
    [Required]
    public Address Sender { get; set; }

    [Required]
    public Address Receiver { get; set; }

    [Required]
    public string CategoryId { get; set; }

    [Required]
    public List<Item> Items { get; set; }
}
