using MediatR;

public sealed record UpdateCommand(
    Guid Id,
    Guid CityId,
    Guid WardId,
    string Street,
    double Latitude,
    double Longitude,
    bool IsDefault,
    string Name,
    string Phone) : IRequest<Guid>;