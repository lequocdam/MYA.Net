public class UpdateWarehouseDto
{
    public string Name { get; set; }

    public string Street { get; set; }

    public Guid WardId { get; set; }

    public Guid CityId { get; set; }

    public double Latitude { get; set; }

    public double Longitude { get; set; }
}