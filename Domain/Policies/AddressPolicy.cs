public static class AddressPolicy
{
    public static void EnsureNotSame(Address from, Address to)
    {
        if (from.Id == to.Id)
            throw new BusinessRuleException(OrderErrors.SameAddress);
    }

    public static void EnsureActive(Address address)
    {
        if (!address.IsActive)
            throw new BusinessRuleException(
                OrderErrors.AddressInactive);
    }

    public static void EnsureServiceAvailable(Address from, Address to)
    {
        if (from.CountryId != to.CountryId)
            throw new BusinessRuleException(
                OrderErrors.ServiceUnavailable);
    }

    public static async Task CanCreateAsync(
        Guid userId,
        CreateAddressCommand command,
        IAddressRepository repository,
        CancellationToken ct)
    {
        var count = await repository.CountByUserAsync(
                userId,
                ct);

        if (count >= 20)
            throw new BusinessRuleException(AddressErrors.MaximumReached);
    }
}