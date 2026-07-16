public sealed class CreateHandler(
    IDriverRepository repository,
    ICurrentUserService currentUserService) : IRequestHandler<CreateCommand, Guid>
{
    public async Task<Guid> Handle(CreateCommand request, CancellationToken ct)
    {
        var currentUser = currentUserService.GetCurrent();

        var driver = Driver.Create(currentUser.Id);

        await repository.AddAsync(driver, ct);

        await repository.SaveChangesAsync(ct);

        return driver.Id;
    }
}