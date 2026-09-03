public class HubService(
    IWarehouseRepository warehouseRepository,
    IMapper mapper,
    ILogger<WarehouseService> logger) : IWarehouseService
{
    public async Task<PagedData<HubResponse>> GetListAsync(
        GetListRequest request,
        CancellationToken ct)
    {
        return await hubRepository.GetListAsync(request, ct);
    }

    public async Task<DetailResponse> GetDetailAsync(Guid id)
    {
        return await hubRepository.GetDetailAsync(id, ct)
            ?? throw new NotFoundException("User not found.");
    }

    public async Task<HubResponse> CreateAsync(
        CreateRequest request,
        CancellationToken ct)
    {
        var hub = Hub.Create(
            request.Name,
            request.Address);

        await hubRepository.AddAsync(hub, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return mapper.Map<HubResponse>(hub);
    }

    public async Task<HubResponse> UpdateAsync(
        Guid id,
        UpdateRequest request,
        CancellationToken ct)
    {
        var hub = await hubRepository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Hub not found.");

        if (hub.IsDeleted)
            throw new NotFoundException("Hub is deleted.");

        hub.Update(
            request.Name,
            request.Address);

        await unitOfWork.SaveChangesAsync(ct);

        return mapper.Map<HubResponse>(hub);
    }

    public async Task DeleteAsync(
        Guid id,
        CancellationToken ct)
    {
        var hub = await hubRepository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Hub not found.");

        if (hub.IsDeleted)
            throw new NotFoundException("Hub is deleted.");

        hub.Delete();

        await unitOfWork.SaveChangesAsync(ct);
    }
}
