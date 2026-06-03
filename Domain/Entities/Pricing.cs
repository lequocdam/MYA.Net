public class Pricing
{
    public int Id { get; set; }
    public Zone Zone { get; set; }
    public decimal BasePrice { get; set; }
    public decimal StepWeight { get; set; }
    public decimal StepFee { get; set; }
    public decimal RemoteFee { get; set; }
    public decimal CodRate { get; set; }
    public DateTime EffectiveDate { get; set; }
}