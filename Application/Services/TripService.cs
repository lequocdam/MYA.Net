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

   public async Task<Guid> CreateAsync(
        CreateTripDto dto,
        CancellationToken ct)
    {
        var orders = await orderRepository.Query()
            .Where(x => dto.OrderIds.Contains(x.Id)).ToListAsync(ct);

        if (orders.Count != dto.OrderIds.Count)
            throw new BadRequestException(
                "Some orders not found.");

        if (orders.Any(x => x.TripId != null))
            throw new BadRequestException(
                "Some orders already assigned.");

        switch (dto.Type)
        {
            case TripType.Pickup:
                ValidatePickup(dto);
                break;

            case TripType.Delivery:
                ValidateDelivery(dto);
                break;

            case TripType.Transfer:
                ValidateTransfer(dto);
                break;
        }

        var trip = new Trip
        {
            Id = Guid.NewGuid(),
            Code = GenerateCode(),
            Type = dto.Type,
            Status = TripStatus.Prepared,
            Date = DateTime.UtcNow,
            FromId = dto.FromId,
            ToId = dto.ToId,
            DriverId = dto.DriverId,
            VehicleId = dto.VehicleId,
            Orders = orders
        };

        if (dto.Type != TripType.Transfer)
        {
            trip.Stops = dto.Stops
                .OrderBy(x => x.Sequence)
                .Select(x => new TripStop
                {
                    Id = Guid.NewGuid(),
                    TripId = tripId,
                    Sequence = x.Sequence,
                    LocationId = x.LocationId
                })
                .ToList();
        }

        foreach (var order in orders)
        {
            order.TripId = tripId;
        }

        await tripRepository.AddAsync(trip, ct);

        return tripId;
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

    public async Task StartAsync(
        Guid tripId,
        Guid driverId,
        CancellationToken ct)
    {
        var trip = await tripRepository
            .Query()
            .FirstOrDefaultAsync(t => t.Id == tripId, ct)
            ?? throw new NotFoundException("Trip not found");

        if (trip.DriverId != driverId)
            throw new ForbiddenException();

        var orders = await orderRepository
            .Where(o => o.TripId == tripId)
            .ToListAsync(ct);

        if (!orders.Any())
                throw new NotFoundException("Orders not found");

        orderService.UpdateOrderStatus()

        await unitOfWork.SaveChangesAsync(ct);
    }
}