public sealed record CreateDriverCommand(
    string Code,
    Guid UserId,
    DriverType Type
) : IRequest<Guid>;