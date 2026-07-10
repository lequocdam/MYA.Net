public sealed class CreateHandler(
    ICurrentUserService currentUserService,
    IAddressRepository addressRepository,
    IAddressFactory addressFactory)
    : IRequestHandler<CreateAddressCommand, Guid>
{
    public async Task<Guid> Handle(
        CreateCommand command,
        CancellationToken ct)
    {
        var currentUser = currentUserService.GetCurrent();

        await AddressPolicy.CanCreateAsync(
            currentUser.Id,
            command,
            addressRepository,
            ct);

        if (command.IsDefault)
        {
            await addressRepository.ClearDefaultAsync(
                currentUser.Id,
                ct);
        }

        var address = addressFactory.Create(
            currentUser.Id,
            command);

        await addressRepository.AddAsync(address, ct);

        await addressRepository.UnitOfWork.SaveChangesAsync(ct);

        return address.Id;
    }
}