public sealed record Quote(
    Guid ServiceId
    Guid ZoneId,
    decimal Weight,
    decimal Cost,
    decimal Fee,
    decimal Total);