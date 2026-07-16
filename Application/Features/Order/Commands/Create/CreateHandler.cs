using MediatR;
using Microsoft.Extensions.Logging;
using YourApp.Application.Orders.Abstractions;
using YourApp.Application.Common.Exceptions;
using YourApp.Domain.Orders;

namespace MYA.Application.Orders.Commands.Create;

public sealed class CreateHandler(
    ICurrentUserService currentUserService,
    IAddressRepository addressRepository,
    IRoutingService routingService, // Đảm bảo Injection hệ thống định tuyến đa kho
    IQuoteService quoteService,
    IWriteOrderService writeOrderService,
    ILogger<CreateHandler> logger) : IRequestHandler<CreateCommand, Guid>
{
    public async Task<Guid> Handle(CreateCommand command, CancellationToken ct)
    {
        var currentUser = currentUserService.GetCurrent();

        var fromTask = addressRepository.GetByIdAsync(command.FromAddressId, ct);
        var toTask = addressRepository.GetByIdAsync(command.ToAddressId, ct);

        await Task.WhenAll(fromTask, toTask);

        var fromAddress = await fromTask ?? throw new NotFoundException("From address");
        var toAddress = await toTask ?? throw new NotFoundException("To address");
            
        AddressPolicy.EnsureDifferent(fromAddress, toAddress);

        // 2. Tạo danh sách Items
        var items = command.Items
            .Select(x => OrderItem.Create(
                x.Name,
                x.WeightKg,
                x.Quantity,
                x.LengthCm,
                x.WidthCm,
                x.HeightCm))
            .ToList();
            
        var route = await routeService.PlanAsync(new RouteRequest(
            fromAddress,
            toAddress, 
            command.ServiceId), ct);

        var quote = await quoteService.CalculateAsync(new QuoteRequest(
            fromAddress,
            toAddress,
            command.ServiceId,
            command.Cod,
            items), ct);

        return await writeOrderService.CreateAsync(new CreateContext(  
            currentUser.Id,
            fromAddress,     // 1. Dữ liệu gốc (Địa chỉ đi)
            toAddress,         // 2. Dữ liệu gốc (Địa chỉ đến)
            command.ServiceId,            // 3. Loại dịch vụ áp dụng
            route.FromWarehouseId,       // 4. Kết quả định tuyến (Kho lấy)
            route.ToWarehouseId,     // 5. Kết quả định tuyến (Kho giao)
            route.FromWarehouseId,      // 6. Vị trí hiện tại (nếu có)
            price: price,                 // 7. Giá tiền
            items: items                  // 8. Danh sách hàng hóa
        ), // Đóng ngoặc của new CreateContext tại đây
        ct // CancellationToken truyền vào hàm CreateAsync
    );
    }
}