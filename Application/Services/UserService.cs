public class UserService(
    IUserRepository repo,
    ILogger<UserService> logger) : IUserService,
{

    public async Task<IEnumerable<UserResponseDto>> GetAll(UserFilterDTO filter, Guid userId)
    {
        var users = repo.SeclectAsync();

        if (!string.IsNullOrWhiteSpace(filter.Phone))
            users = repo.SelectByPhoneAsync(users, filter.Phone);

        if (!string.IsNullOrWhiteSpace(filter.Email))
            users = repo.SelectByEmailAsync(users, filter.Email);

        if (filter.Role.HasValue)
            users = repo.SelectByRoleAsync(users, filter.Role.Value);

        if (filter.From.HasValue)
            users = repo.SelectByFromAsync(users, filter.From.Value);

        if (filter.To.HasValue)
            users = repo.SelectByToAsync(users, filter.To.Value);

        var total = await query.CountAsync();

        var orders = await query
            .OrderByDescending(o => o.Date)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(o => new OrderDTO
            {
                Id     = o.Id,
                Code   = o.Code,
                ReceiverName  = o.Receiver.Name,
                ReceiverPhone = o.Receiver.Phone,
                Category = o.Category,
                Total  = o.Total,
                Status = o.Status,
                Date   = o.Date,
            })
            .ToListAsync();
    }

    public async Task<UserResponseDto> GetMe(ClaimsPrincipal principal)
    {
        var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null) return null;

        var user = await _context.Users.FindAsync(Guid.Parse(userId));
        if (user == null) return null;

        return new UserResponseDto
        {
            Id = user.Id,
            Phone = user.Phone,
            Name = user.Name
        };
    }

    public async Task<UserDTO> CreateAsync(CreateUserDTO dto)
    {
        var exist = await repo.AnyAsync(dto.Phone, dto.Email);
        if (exist)
            throw new ConflictException("Phone or email created");

        var hash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Phone = dto.Phone,
            Email = dto.Email,
            Password = hash,
            Role = dto.Role ?? "user",
        };

        logger.LogInformation("{Id}, {Role} created", user.Id, user.Role);

        await repo.Add(user);
        await repo.SaveChangesAsync();

        return new ApiResponse<CreateUserResDTO>{
            Message = "Account created",
            Data = {
                user.Id, 
                user.Name,
                user.Phone,
                user.Email,
                user.Role,
            },
        };
    }

    public async Task<UserResDTO> UpdateAsync(UpdateUserDTO dto, Guid userId)
    {
        var user = await repo.FirstOrDefaultAsync(userId);
        if (user is null)
            throw new NotFoundException("Account not found");

        user.Name = dto.Name;
        user.Phone = dto.Phone;
        user.Email = dto.Email;

        await repo.SaveChangesAsync();

        return new ApiResponse<UpdateUserResDTO>{
            Message = "Account updated",
            Data = {
                user.Id, 
                user.Name,
                user.Phone,
                user.Email,
                user.Role,
            },
        };
    }

    public async Task<(bool Success, string Message)> ChangePasswordAsync(ClaimsPrincipal principal, ChangePasswordDTO dto)
    {
        var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null) return (false, "Unauthorized");

        var user = await _context.Users.FindAsync(Guid.Parse(userId));
        if (user == null) return (false, "User not found");

        if (!BCrypt.Net.BCrypt.Verify(dto.OldPassword, user.PasswordHash))
            return (false, "Old password is incorrect");

        if (dto.NewPassword != dto.ConfirmPassword)
            return (false, "Passwords do not match");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        await _context.SaveChangesAsync();

        return (true, "Password changed successfully");
    }
}