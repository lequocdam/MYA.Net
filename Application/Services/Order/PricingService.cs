using Domain.Enums;
using Domain.Interfaces;
using Domain.ValueObjects;

public class PriceService : IPriceService
{
    private readonly IPriceEngine _priceEngine;

    public PriceService(IPriceEngine priceEngine)
    {
        _priceEngine = priceEngine;
    }

    public async Task<Price> Calculate(
        double weight,
        Zone zone,
        double cod = 0)
    {
        return await _priceEngine.Calculate(
            weight,
            zone,
            cod);
    }
}