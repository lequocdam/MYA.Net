public class OrderOrchestrator
{
    private readonly AppDbContext _context;
    private readonly OrderFactory _factory;

    public OrderOrchestrator(AppDbContext context, OrderFactory factory)
    {
        _context = context;
        _factory = factory;
    }

    public async Task<object> CreateOrder(CreateOrderDTO dto)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var (pricing, flow) = _factory.Resolve(dto.ServiceId);

            var cost = pricing.Calculate(dto);
            var fee = 15000;
            var total = cost + fee;

            var sender = new Address
            {
                Id = Guid.NewGuid().ToString(),
                Name = dto.Sender.Name,
                Phone = dto.Sender.Phone,
                Email = dto.Sender.Email,
                Ward = dto.Sender.Ward,
                City = dto.Sender.City
            };

            var receiver = new Address
            {
                Id = Guid.NewGuid().ToString(),
                Name = dto.Receiver.Name,
                Phone = dto.Receiver.Phone,
                Email = dto.Receiver.Email,
                Ward = dto.Receiver.Ward,
                City = dto.Receiver.City
            };

            _context.Addresses.AddRange(sender, receiver);
            await _context.SaveChangesAsync();

            var order = new Order
            {
                Id = Guid.NewGuid().ToString(),
                Code = GenerateCode(),
                SenderId = sender.Id,
                ReceiverId = receiver.Id,
                Service = dto.ServiceId,
                Warehouse = dto.WarehouseId,
                Note = dto.Note,
                Cost = cost,
                Fee = fee,
                Total = total
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // 5. Lưu trạng thái đầu tiên
            var firstStatus = flow.GetFlow().First();

            _context.OrderStatuses.Add(new OrderStatus
            {
                Id = Guid.NewGuid().ToString(),
                OrderId = order.Id,
                Status = firstStatus,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            return new
            {
                order.Code,
                order.Total,
                status = firstStatus
            };
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private string GenerateCode()
    {
        return $"MYA-{DateTime.UtcNow:yyyyMMddHHmmss}";
    }
}