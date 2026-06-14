public class Pricing
{
    public Guid Id { get; set; }

    public decimal BaseWeight { get; set; }

    public decimal BaseCost { get; set; }

    public decimal NextWeight { get; set; }

    public decimal AddedCost { get; set; }

    public Guid ServiceId { get; set; }

    public Guid ZoneId { get; set; }
}