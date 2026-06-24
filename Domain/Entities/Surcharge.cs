public class Surcharge
{
    public Guid Id { get; set; }

    public string? Name { get; set; };

    public SurchargeType Type { get; set; }

    public decimal Value { get; set; }

    public decimal Percent { get; set; }

    public bool IsActive { get; set; }
}