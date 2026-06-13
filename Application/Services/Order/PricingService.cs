using Domain.Enums;

public class PriceService(
    IPriceEngine priceEngine) : IPriceService
{

    public async Task<Price> Calculate(
        decimal weight,
        Zone zone)
    {
        return await priceEngine.Calculate(
            weight,
            zone);
    }
}