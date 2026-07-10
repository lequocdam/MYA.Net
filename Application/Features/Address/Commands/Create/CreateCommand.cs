public sealed record CreateCommand(
    Guid CityId,
    Guid WardId,
    string Name,
    string Phone,
    string Street,
    double Latitude,
    double Longitude,
    bool IsDefault) : IRequest<Guid>;