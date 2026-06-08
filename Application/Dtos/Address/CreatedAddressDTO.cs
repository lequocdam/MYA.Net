using System.ComponentModel.DataAnnotations;

public class AddressDTO
{
    [Required]
    public string Name { get; set; }

    [Required]
    public string Phone { get; set; }

    [Required]
    public string Email { get; set; }

    [Required]
    public string Address { get; set; }
}
