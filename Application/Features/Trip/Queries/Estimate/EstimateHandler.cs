public class EstimateHandler : IRequestHandler<EstimateQuery, EstimateDto>
{
    private readonly IZoneService zoneService;
    private readonly IWeightService weightService;
    private readonly IPriceService priceService;

    public async Task<EstimateDto> Handle(
        EstimateQuery request,
        CancellationToken ct)
    {
        var dto = request.Dto;

        var zone = zoneService.GetZone(dto.Sender, dto.Receiver);

        var weight = weightService.Calculate(dto.Items);

        var price = priceService.Calculate(zone, weight);

        var deliveryDays = zone switch
        {
            "Internal" => 1,
            "SameRegion" => 2,
            "CrossRegion" => 4,
            _ => 5
        };

        return new EstimateDto
        {
            Zone = zone,
            Weight = weight,
            Cost = price.Cost,
            Fee = price.Fee,
            Total = price.Total,
            EstimatedDeliveryDays = deliveryDays
        };
    }
}