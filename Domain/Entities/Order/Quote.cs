public sealed record Quote(
    Guid ServiceId
    Guid ZoneId,
    decimal Weight,
    Price price);