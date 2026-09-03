public class UserService : IUserService
{
    public async Task<PagedResult<UserResponse>> GetUsersAsync(
        GetUsersRequest request,
        CancellationToken ct)
    {
        return await userRepository.GetUsersAsync(request, ct);
    }

    public async Task<UserResponse> GetByIdAsync(Guid id)
    {
        return await userRepository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("User not found.");
    }

    public async Task<UserResponse> CreateAsync(
        CreateRequest request,
        CancellationToken ct)
    {
        var exist = await userRepository.ExistAsync(
            request.Email,
            request.Phone,
            ct);

        userPolicy.CanCreate(exist);

        var hashPassword = passwordHasher.Hash(request.Password);

        var user = User.Create(
            request.Name,
            request.Email,
            request.Phone,
            hashPassword);

        await userRepository.AddAsync(user, ct);

        await unitOfWork.SaveChangesAsync(ct);

        return mapper.Map<UserResponse>(user);
    }

    public async Task<UserResponse> UpdateAsync(
        Guid id,
        UpdateRequest request,
        CancellationToken ct)
    {
        var user = await userRepository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("User not found.");

        user.Update(
            request.Name,
            request.Email,
            request.Phone);

        await unitOfWork.SaveChangesAsync(ct);

        return mapper.Map<UpdateResponse>(user);
    }

    public async Task DeleteAsync(
        Guid id,
        CancellationToken ct)
    {
        var user = await userRepository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("User not found.");

         user.Delete();

        await unitOfWork.SaveChangesAsync(ct);
    }
}
