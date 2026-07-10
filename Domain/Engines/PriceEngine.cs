using YourApp.Domain.Pricing.Entities;
using YourApp.Domain.Pricing.ValueObjects;

namespace YourApp.Domain.Pricing.Services;

public class PricingCalculator
{
    public DeliveryPrice Calculate(TariffConfig tariff, decimal chargeableWeightKg, decimal codAmount)
    {
        // 1. Tính toán từng cấu phần chi phí độc lập
        var baseCost = CalculateBaseCost(tariff, chargeableWeightKg);
        var codFee = CalculateCodFee(tariff, codAmount);
        var surchargeFee = CalculateTotalSurcharge(tariff, baseCost);
        
        // 2. Sửa lỗi: Tổng chi phí bắt buộc phải cộng dồn toàn bộ các cấu phần
        var totalPrice = baseCost + codFee + surchargeFee;

        return new DeliveryPrice(
            BaseCost: baseCost,
            CodFee: codFee,
            SurchargeFee: surchargeFee,
            TotalPrice: totalPrice
        );
    }

    private static decimal CalculateCost(TariffConfig tariff, decimal chargeableWeightKg)
    {
        if (chargeableWeightKg <= tariff.FirstWeightKg)
            return tariff.FirstCost;

        var extraWeightKg = chargeableWeightKg - tariff.BaseWeightKg;
        if (extraWeightKg <= 0) return tariff.BaseCost;

        var extraSteps = Math.Ceiling(extraWeightKg / tariff.WeightStepKg);

        return tariff.BaseCost + (extraSteps * tariff.ExtraStepCost);
    }

    // Thuật toán tính phí thu hộ COD dựa trên tỷ lệ phần trăm hoặc mức sàn tối thiểu
    private static decimal CalculateCodFee(TariffConfig tariff, decimal codAmount)
    {
        if (codAmount <= 0) return 0m;
        
        // Trong thực tế, CodRate thường cấu hình dạng phần trăm (Ví dụ: 0.5% hoặc 1%)
        // Do đó bắt buộc phải chia cho 100 để ra tỷ lệ thập phân chính xác
        var calculatedFee = (codAmount * tariff.CodRatePercentage) / 100m; 
        
        // Hãng vận chuyển luôn lấy con số lớn hơn giữa tỷ lệ % và mức phí sàn tối thiểu
        return Math.Max(calculatedFee, tariff.MinCodFee);
    }

    // Thuật toán duyệt danh sách phụ phí (Phí nhiên liệu, phí vùng xa...)
    private static decimal CalculateTotalSurcharge(TariffConfig tariff, decimal baseShippingCost)
    {
        if (tariff.Surcharges == null || !tariff.Surcharges.Any()) return 0m;

        decimal totalSurcharge = 0m;

        foreach (var surcharge in tariff.Surcharges)
        {
            if (!surcharge.IsActive)
                continue;

            // Nếu là phí tính theo %, nhân với giá cước gốc (baseShippingCost) và chia 100
            totalSurcharge += surcharge.IsPercentage
                ? (baseShippingCost * surcharge.ValuePercentage) / 100m
                : surcharge.FixedValue;
        }

        return totalSurcharge;
    }
}