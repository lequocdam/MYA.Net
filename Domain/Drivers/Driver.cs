public sealed class Driver : AggregateRoot<Guid>
{
    public Guid UserId { get; private set; }

    public DriverStatus Status { get; private set; }

    public Guid? VehicleId { get; private set; }

    public bool IsActive { get; private set; }

    public DateTime CreatedAt { get; private set; }

    private Driver()
    {
    }

    public static Driver Create(Guid userId)
    {
        return new Driver
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Status = DriverStatus.Ready,
            Date = DateTime.UtcNow
        };
    }

    public void AssignVehicle(Guid vehicleId)
    {
        VehicleId = vehicleId;
    }

    public void RemoveVehicle()
    {
        VehicleId = null;
    }

    public void MarkBusy()
    {
        if (Status == DriverStatus.Busy)
            return;

        Status = DriverStatus.Busy;
    }

    public void MarkAvailable()
    {
        Status = DriverStatus.Available;
    }

    public void Deactivate()
    {
        IsActive = false;
        Status = DriverStatus.Offline;
    }

    public void Activate()
    {
        IsActive = true;
        Status = DriverStatus.Available;
    }
}