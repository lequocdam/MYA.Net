using Microsoft.EntityFrameworkCore;
using MediatR;
using AutoMapper;

public sealed class UpdateHandler(
    IAddressRepository addressRepository,
    ICurrentUserService currentUserService,
    IMapper mapper) : IRequestHandler<UpdateCommand, Guid>
{
    public async Task<Guid> Handle(
        UpdateCommand command,
        CancellationToken ct)
    {
        var currentUser = currentUserService.Get();
        var entity = await addressRepository.FirstOrDefaultAsync(command.Id, ct)
            ?? throw new NotFoundException("Address not found");

        AddressPolicy.ValidateUpdate(currentUser, entity);

        if (request.IsDefault)
            await addressRepository.ClearDefaultAsync(currentUser, ct);

        var address = mapper.Map<Address>(request);
        var contact = mapper.Map<Contact>(request);
        addressEntity.Update(address, contact);

        await addressRepository.SaveChangesAsync(ct);

        return addressEntity.Id;
    }
}