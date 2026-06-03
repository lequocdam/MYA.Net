using Domain.Entities;
using Domain.Interfaces;
using Domain.ValueObjects;
using Domain.Enums;

public class PriceEngine : IPriceEngine
{
    private readonly IRuleProvider _ruleProvider;
    private readonly IFeeCalculator _feeCalculator;
    private readonly ISurchargeCalculator _surchargeCalculator;

    public PriceEngine(
        IRuleProvider ruleProvider,
        IFeeCalculator feeCalculator,
        ISurchargeCalculator surchargeCalculator)
    {
        _ruleProvider = ruleProvider;
        _feeCalculator = feeCalculator;
        _surchargeCalculator = surchargeCalculator;
    }

    public async Task<Price> Calculate(
        double weight,
        Zone zone,
        double cod = 0)
    {
        if (weight <= 0)
            throw new ArgumentException("Weight invalid");

        var rule = await _ruleProvider.Get(zone, weight);

        if (rule == null)
            throw new Exception("No pricing rule");

        var cost = _feeCalculator.Calculate(rule);

        var fee = _surchargeCalculator.Calculate(rule);

        return new Price
        {
            Cost = cost,
            Fee = fee
        };
    }
}