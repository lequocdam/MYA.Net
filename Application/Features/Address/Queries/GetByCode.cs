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

namespace MYA.Application.Features.Orders.Queries.GetOrders;

public sealed class GetOrdersQueryHandler(
    IRepository<Order> repository,
    IMapper mapper)
    : IRequestHandler<GetOrdersQuery, OrderRes>
{
    public async Task<OrderRes> Handle(
        GetOrdersQuery request,
        CancellationToken ct)
    {
        var spec = new OrderByIdSpecification(request.Code);

        var order = await repository.FirstOrDefaultAsync();

        return new OrderRes(
            request.FilterRequest.Page,
            request.FilterRequest.PageSize,
            total,
            mapper.Map<List<OrderDto>>(orders));
    }
}