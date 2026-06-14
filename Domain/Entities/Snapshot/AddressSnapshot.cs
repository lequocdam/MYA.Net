public class AddressSnapshot
{
    public string Name { get; set; } = null!;
    public string Phone { get; set; } = null!;
    public string Province { get; set; } = null!;
    public string District { get; set; } = null!;
    public string Ward { get; set; } = null!;
    public string Street { get; set; } = null!;
    public double Latitude { get; set; }
    public double Longitude { get; set; }

    public Guid   Id        { get; set; }
    public string Name      { get; set; }
    public string Phone     { get; set; }
    public string City      { get; set; }
    public string Ward      { get; set; }
    public string Street    { get; set; }
    public double Latitude  { get; set; }
    public double Longitude { get; set; }
    public Guid   UserId    { get; set; }
}