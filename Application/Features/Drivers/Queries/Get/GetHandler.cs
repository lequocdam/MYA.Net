public sealed class GetHandler(
    IDriverRepository repository,
    IMapper mapper)
    : IRequestHandler<GetQuery, PagedResult<DriverDto>>
{
    public async Task<PagedResult<DriverDto>> Handle(
        GetQuery query,
        CancellationToken ct)
    {
        return await repository.GetAllAsync(
            new DriverSpecification(request),
            request.Page,
            request.PageSize,
            mapper,
            ct);
    }
}