public class Warehouse
{
    public Guid     Id        { get; set; }
    public string   Name      { get; set; }
    public string   Address   { get; set; }
    public double   Latitude  { get; set; }
    public double   Longitude { get; set; }
    public string   Province  { get; set; }
    public string   District  { get; set; }
    public bool     IsDefault { get; set; }
    public bool     IsActive  { get; set; }

    public ICollection<WarehouseCoverage> Coverages { get; set; }
}

public class WarehouseCoverage
{
    public Guid   Id          { get; set; }
    public Guid   WarehouseId { get; set; }
    public string Province    { get; set; }
    public string District    { get; set; }

    public Warehouse Warehouse { get; set; }
}
