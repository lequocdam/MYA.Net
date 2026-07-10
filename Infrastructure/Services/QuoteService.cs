using YourApp.Domain.Orders.ValueObjects;
using YourApp.Domain.Orders.Entities;
using YourApp.Domain.Orders.Services;

namespace YourApp.Infrastructure.Services;

public class QuoteService(
    IPricingService pricingService,
    IZoneService zoneService,
    IWeightService weightService) : IQuoteService
{
    public async Task<Quote> GetAsync(
        PricingContext context,
        CancellationToken ct)
    {
        var zone = await zoneService.GetAsync(fromAddress, toAddress, ct);

        var weight = await weightService.CalculateAsync(context.Items, ct);

        var price = await pricingService.CalculateAsync(
            context.ServiceId, 
            zone.Id, 
            weight, // Tên biến thể hiện rõ đây là cân nặng tính cước đo bằng Kg
            context.CodAmount, 
            ct);

        // 4. Sửa lỗi: Trả về đúng biến calculatedPrice đã khai báo ở trên
        return new Quote(
            serviceId,
            zone.Id,
            chargeableWeightKg,
            calculatedPrice
        );
    }
}
