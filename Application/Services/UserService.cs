using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MYAlog.Application.Services;

public class UserService(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IUnitOfWork unitOfWork,
    IMapper mapper,
    ILogger<UserService> logger) : IUserService
{
    public async Task<PagedData<UserResponse>> GetListAsync(
        GetListRequest request,
        CancellationToken ct)
    {
        if(!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var keyword = request.Keyword.Trim();
        }

        var pagedUsers = await userRepository.GetListAsync(request, ct);

        return new PagedData<UserResponse>
        {
            Items = mapper.Map<List<UserResponse>>(pagedUsers.Items),
            TotalCount = pagedUsers.TotalCount,
            PageIndex = pagedUsers.PageIndex,
            PageSize = pagedUsers.PageSize
        };
    }

    public async Task<DetailResponse> GetDetailAsync(
        Guid id,
        CancellationToken ct)
    {
        var detail = await userRepository.GetDetailAsync(id, ct)
            ?? throw new NotFoundException("User detail not found.");

        return mapper.Map<DetailResponse>(detail);
    }

    public async Task<UserResponse> CreateAsync(
        CreateRequest request,
        CancellationToken ct)
    {
        var existed = await userRepository.ExistAsync(request.Email, request.Phone, ct);

        if (existed)
            throw new ConflictException("Email or phone existed.");

        var hashedPassword = await passwordHasher.Hash(request.Password);

        var user = User.Create(request.Name, request.Email, request.Phone, hashedPassword);
        await userRepository.AddAsync(user, ct);
        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation("User {UserId} created.", user.Id);
        return mapper.Map<UserResponse>(user);
    }

    public async Task UpdateAsync(
        Guid id,
        UpdateRequest request,
        CancellationToken ct)
    {
        var user = await userRepository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("User not found.");

        var existed = await userRepository.ExistAsync(request.Email, request.Phone, ct);

        if (existed)
            throw new ConflictException("Email or phone existed.");

        user.Update(request.Name, request.Email, request.Phone);
        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation("User {UserId} updated.", user.Id);
    }

    public async Task ChangeStatusAsync(
        Guid id,
        UserStatus status,
        CancellationToken ct)
    {
        var user = await userRepository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("User not found.");

        user.ChangeStatus(status);
        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation("User {UserId} changed to {Status}.", user.Id, status);
    }

    public async Task UnlockAsync(
        Guid id,
        CancellationToken ct)
    {
        var user = await userRepository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("User not found.");

        user.Unlock();
        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation("User {UserId} unlocked.", user.Id);
    }

    public async Task DeleteAsync(
        Guid id,
        CancellationToken ct)
    {
        var user = await userRepository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("User not found.");

        user.Delete();
        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation($"User {user.Id} deleted.");
    }
}
