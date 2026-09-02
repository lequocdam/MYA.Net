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

    public async Task ResetPasswordAsync(
        Guid id,
        ResetPasswordRequest request,
        CancellationToken ct)
    {
        var user = await userRepository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("User not found.");

        var passwordHash = passwordHasher.Hash(request.NewPassword);

        user.ResetPassword(passwordHash);

        await unitOfWork.SaveChangesAsync(ct);
    }


    public async Task AssignRolesAsync(Guid userId, List<Guid> roleIds, Guid actorUserId)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId)
            ?? throw new NotFoundException("User không tồn tại");

        var validRoleIds = await _db.Roles
            .Where(r => roleIds.Contains(r.Id))
            .Select(r => r.Id)
            .ToListAsync();

        if (validRoleIds.Count != roleIds.Distinct().Count())
            throw new BusinessValidationException("Có RoleId không tồn tại trong danh sách gửi lên");

        // Replace toàn bộ role hiện tại bằng danh sách mới trong 1 transaction
        var existing = _db.UserRoles.Where(ur => ur.UserId == userId);
        _db.UserRoles.RemoveRange(existing);

        foreach (var roleId in validRoleIds)
        {
            _db.UserRoles.Add(new UserRole
            {
                UserId = userId,
                RoleId = roleId,
                AssignedAt = DateTime.UtcNow,
                AssignedBy = actorUserId
            });
        }

        await _db.SaveChangesAsync();
    }

    public async Task<List<string>> GetUserRoleCodesAsync(Guid userId)
    {
        return await _db.UserRoles
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.Role.Code)
            .ToListAsync();
    }

    // ---- Helpers ----

    private async Task AttachRolesAsync(Guid userId, List<Guid> roleIds, Guid actorUserId)
    {
        foreach (var roleId in roleIds.Distinct())
        {
            _db.UserRoles.Add(new UserRole
            {
                UserId = userId,
                RoleId = roleId,
                AssignedAt = DateTime.UtcNow,
                AssignedBy = actorUserId
            });
        }
    }

    private async Task EnsureUniqueAsync(string username, string email, string phoneNumber)
    {
        var exists = await _db.Users.AnyAsync(u =>
            u.Username == username || u.Email == email || u.PhoneNumber == phoneNumber);

        if (exists)
            throw new ConflictException("Username, email hoặc số điện thoại đã tồn tại");
    }

    private static IQueryable<User> ApplySort(IQueryable<User> q, string sortBy, string sortDir)
    {
        var desc = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);

        return sortBy.ToLower() switch
        {
            "fullname" => desc ? q.OrderByDescending(u => u.FullName) : q.OrderBy(u => u.FullName),
            "username" => desc ? q.OrderByDescending(u => u.Username) : q.OrderBy(u => u.Username),
            _ => desc ? q.OrderByDescending(u => u.CreatedAt) : q.OrderBy(u => u.CreatedAt),
        };
    }

    private static UserDto MapToDto(User u) => new()
    {
        Id = u.Id,
        Username = u.Username,
        Email = u.Email,
        PhoneNumber = u.PhoneNumber,
        FullName = u.FullName,
        AvatarUrl = u.AvatarUrl,
        Status = u.Status.ToString(),
        BranchId = u.BranchId,
        Roles = u.UserRoles.Select(ur => ur.Role.Code).ToList(),
        CreatedAt = u.CreatedAt
    };
}
