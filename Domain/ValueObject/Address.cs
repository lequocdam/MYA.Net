public sealed record Address(
    Guid CityId,
    Guid WardId,
    string Street,
    double Latitude,
    double Longitude,
    bool IsDefault);