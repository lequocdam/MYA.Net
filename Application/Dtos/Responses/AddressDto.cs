public class AddressDto
{
    public Guid   Id        { get; set; }

    public string Name      { get; set; }

    public string Phone     { get; set; }

    public string City      { get; set; }

    public string Ward      { get; set; }

    public string Street    { get; set; }

    public bool   IsDefault { get; set; }
}
