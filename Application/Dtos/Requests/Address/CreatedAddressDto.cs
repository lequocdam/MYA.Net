public class CreateAddressDto
{
    public string Name      { get; set; }

    public string Phone     { get; set; }

    public string City      { get; set; }

    public string Ward      { get; set; }

    public string Street    { get; set; }

    public double Latitude  { get; set; }

    public double Longitude { get; set; }

    public bool   IsDefault { get; set; }
}
