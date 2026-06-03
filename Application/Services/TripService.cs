public class TripService : ITripService
{
    private readonly AppDbContext _context;
    private readonly IAddressService _addressService;
    private readonly IPriceService _priceService;

    public OrderService(
        AppDbContext context,
        IAddressService addressService,
        IPriceService priceService)
    {
        _context = context;
        _addressService = addressService;
        _priceService = priceService;
    }

    public async Task<Guid> Create(CreateTrip dto)
    {
        var trip = new Trip
        {
            Id = Guid.NewGuid(),
            Code = $"MYA-{DateTime.UtcNow.Ticks}",
            Type = dto.Type,
            FromId = dto.FromId,
            ToId = dto.ToId,
            Status = TripStatus.PREPARED,
            Date = DateTime.UtcNow,
            Orders = dto.Orders.Select(o => new Order
            {
                OrderId = o
            }).ToList()
        };

        _context.Trips.Add(trip);
        await _context.SaveChangesAsync();

        return trip.Id;
    }

    public async Task Assign(Guid tripId, AssignDriverDTO dto)
    {
        var trip = await _context.Trips.FindAsync(tripId);
        var workflow = new TripWorkflow(trip.Status);

        if (!workflow.Can(trigger))
            throw new Exception($"Invalid transition: {order.Status} -> {trigger}");

        var newStatus = workflow.Fire(trigger);

        trip.DriverId = dto.DriverId;
        trip.VehicleId = dto.VehicleId;
        trip.Status = newStatus;

        await _context.SaveChangesAsync();
    }

    public async Task UpdateStatus(Guid orderId, string trigger, string by)
    {
        using var tx = await _context.Database.BeginTransactionAsync();

        var order = await _context.Orders.FindAsync(orderId);
        if (order == null)
            throw new Exception("Order not found");

        var workflow = new OrderWorkflow(order.Status);

        if (!workflow.Can(trigger))
            throw new Exception($"Invalid transition: {order.Status} -> {trigger}");

        var newStatus = workflow.Fire(trigger);

        // update main status
        order.Status = newStatus;

        // save history
        _context.OrderStatusHistories.Add(new OrderStatusHistory
        {
            OrderId = orderId,
            Status = newStatus,
            UpdatedBy = by,
            CreatedAt = DateTime.UtcNow,
            Note = trigger
        });

        await _context.SaveChangesAsync();

        // 🔥 publish event (giống GHN)
        await _eventBus.Publish(new OrderStatusChangedEvent
        {
            OrderId = orderId,
            Status = newStatus
        });

        await tx.CommitAsync();
    }
    
}