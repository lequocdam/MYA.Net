using MYA.Domain.Common.Exceptions;
using MYA.Domain.Orders.Entities;

namespace MYA.Domain.Policies;

public static class WeightPolicy
{
    private const decimal MaxSinglePackageWeightKg = 50.0m; // Đổi sang decimal cho đồng bộ tài chính
    private const decimal MaxDimensionCm = 150.0m;

    public static void ValidateDimensions(IEnumerable<OrderItem> items)
    {
        if (items == null || !items.Any()) return;

        decimal totalActualWeightKg = 0;
        decimal totalEstimatedVolumeCubicCm = 0;

        foreach (var item in items)
        {
            if (item.Weight <= 0 || item.Length <= 0 || item.Width <= 0 || item.Height <= 0)
            {
                throw new DomainException($"");
            }

            // 2. Chốt chặn: Bản thân 1 sản phẩm thô không được phép to hơn thùng xe
            if (item.Length > MaxDimensionCm || item.Width > MaxDimensionCm || item.Height > MaxDimensionCm)
            {
                throw new DomainException($"Mặt hàng {item.Name} có kích thước vượt quá giới hạn tối đa cho phép ({MaxDimensionCm}cm) của hãng.");
            }

            // Cộng dồn để kiểm tra tổng thể đơn hàng
            totalActualWeightKg += item.Weight * item.Quantity;
            totalEstimatedVolumeCubicCm += (item.Length * item.Width * item.Height) * item.Quantity;
        }

        // 3. Chốt chặn: Tổng cân nặng thực tế vượt quá tải trọng của 1 shipper đi xe máy
        if (totalActualWeightKg > MaxSinglePackageWeightKg)
        {
            throw new DomainException($"Tổng khối lượng đơn hàng ({totalActualWeightKg}kg) đã vượt quá giới hạn vận chuyển ({MaxSinglePackageWeightKg}kg). Vui lòng tách làm nhiều đơn.");
        }

        // 4. Chốt chặn: Ước tính kích thước thùng hàng sau đóng gói (Luật thực tế cao cấp)
        // Thuật toán bóc căn bậc 3 để ước lượng cạnh của một chiếc thùng hình lập phương chứa toàn bộ số hàng trên
        decimal estimatedBoxSideCm = (decimal)Math.Pow((double)totalEstimatedVolumeCubicCm, 1.0 / 3.0);
        if (estimatedBoxSideCm > MaxDimensionCm)
        {
            throw new DomainException($"Tổng thể tích hàng hóa quá lớn, kích thước thùng hàng dự kiến vượt quá {MaxDimensionCm}cm. Vui lòng giảm số lượng hoặc tách đơn.");
        }
    }
}
