public class Pricing
{
    public Guid Id { get; set; }

    public Guid ServiceId { get; set; }

    public Guid ZoneId { get; set; }

    public decimal FirstWeight { get; set; }

    public decimal FirstCost { get; set; }

    public decimal NextWeight { get; set; }

    public decimal NextCost { get; set; }
    
    public decimal MinCod { get; set; }

    public decimal CodRate { get; set; }

    public ICollection<Surcharge> Surcharges { get; set; } = new List<Surcharge>();
}