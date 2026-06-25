public class Trip
{
    public Guid Id { get; private set; }

    public string Code { get; private set; } = null!;

    public TripType Type { get; private set; }

    public TripStatus Status { get; private set; }

    public Guid DriverId { get; private set; }

    public Guid VehicleId { get; private set; }

    public DateTime PlannedDate { get; private set; }

    public ICollection<TripOrder> Orders { get; private set; }
        = new List<TripOrder>();

    public ICollection<TripStop> Stops { get; private set; }
        = new List<TripStop>();

    private Trip() { }

    public async Task<Guid> CreateAsync(
        CreateTripDto dto,
        CancellationToken ct)
    {
        var orders = await orderRepository.Query()
            .Where(o => dto.OrderIds.Contains(o.Id)).ToListAsync(ct);

        if (orders.Count != dto.OrderIds.Count)
            throw new BadRequestException("Some orders not found.");

        if (orders.Any(x => x.TripId != null))
            throw new BadRequestException(
                "Some orders already belong to another trip.");

        if (!dto.Stops.Any())
            throw new BadRequestException(
                "Trip must contain at least one stop.");

        var trip = new Trip
        {
            Id = Guid.NewGuid();,
            Code = GenerateCode(),
            Date = DateTime.UtcNow,
            Type = dto.Type,
            Status = TripStatus.Prepared,
            DriverId = dto.DriverId,
            VehicleId = dto.VehicleId,

            Stops = dto.Stops
                .OrderBy(x => x.Sequence)
                .Select(x => new TripStop
                {
                    Id = Guid.NewGuid(),
                    TripId = tripId,
                    Sequence = x.Sequence,
                    LocationId = x.LocationId,
                    Type = x.Type
                })
                .ToList(),

            Orders = orders
        };

        foreach (var order in orders)
        {
            order.TripId = tripId;
        }

        await tripRepository.AddAsync(trip, ct);

        return tripId;
    }

    public void Start()
    {
        if (Status != TripStatus.Prepared)
            throw new DomainException("Trip cannot be started.");

        Status = TripStatus.InProgress;
    }

    public void Complete()
    {
        if (Status != TripStatus.InProgress)
            throw new DomainException("Trip cannot be completed.");

        Status = TripStatus.Completed;
    }

    private static string GenerateCode()
    {
        return $"TRP-{DateTime.UtcNow:yyyyMMddHHmmss}";
    }
}