public record EstimateQuery(
    EstimateDto Dto) : IRequest<EstimateDto>;

    at > /home/claude/OrderService.Cqrs/Queries/GetOrders/GetOrdersQuery.cs << 'EOF'
using MediatR;

namespace YourApp.Application.Orders.Queries.GetOrders;

/// <summary>Danh sách order có phân trang/lọc (tương ứng OrderService.GetOrdersAsync gốc).</summary>
public sealed record GetOrdersQuery(
    OrderFilterReq FilterRequest,
    CurrentUser CurrentUser
) : IRequest<OrderRes>;
EOF

cat > /home/claude/OrderService.Cqrs/Queries/GetOrders/GetOrdersQueryHandler.cs << 'EOF'
using MediatR;

namespace YourApp.Application.Orders.Queries.GetOrders;

public sealed class GetOrdersQueryHandler(
    IOrderRepository orderRepository,
    IMapper          mapper
) : IRequestHandler<GetOrdersQuery, OrderRes>
{
    public async Task<OrderRes> Handle(GetOrdersQuery request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request.FilterRequest);
        ArgumentNullException.ThrowIfNull(request.CurrentUser);

        var spec = new OrderFilterSpecification(request.FilterRequest, request.CurrentUser);

        var count = await orderRepository.CountAsync(spec, ct);
        if (count == 0)
        {
            return new OrderRes(request.FilterRequest.Page, request.FilterRequest.PageSize, 0, new List<OrderDto>());
        }

        var orders = await orderRepository.ToListAsync(spec, ct);
        var result = mapper.Map<List<OrderDto>>(orders);

        // Lưu ý: bản gốc trả về `total` chưa khai báo và kiểu OrderPageDto không khớp
        // với kiểu trả về OrderRes — đã sửa lại dùng `count` và đúng kiểu OrderRes.
        return new OrderRes(request.FilterRequest.Page, request.FilterRequest.PageSize, count, result);
    }
}
EOF
echo ok
Output

ok

Tạo GetOrderByIdQuery + Handler