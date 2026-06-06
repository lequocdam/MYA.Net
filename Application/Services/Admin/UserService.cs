public class UserService(
    IUserRepository userRepo,
    IFileService fileService,
    ILogger<UserService> logger) : IUserService
{

    // ALL
    public async Task<Page<UserDto>> AllAsync(UserFilterDto filter, CancellationToken ct)
    {
        var query = userRepo.Query();

        if (!string.IsNullOrWhiteSpace(filter.Name))
            query = query.Where(u => u.Name.Contains(filter.Name));
 
        if (!string.IsNullOrWhiteSpace(filter.Phone))
            query = query.Where(u => u.Phone.Contains(filter.Phone));
 
        if (!string.IsNullOrWhiteSpace(filter.Email))
            query = query.Where(u => u.Email.Contains(filter.Email));
 
        if (filter.Role.HasValue)
            query = query.Where(u => u.Role == filter.Role.Value);
 
        var total = await query.CountAsync(ct);
 
        var users = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(u => new UserDto
            {
                Id        = u.Id,
                Avatar    = u.Avatar,
                Name      = u.Name,
                Phone     = u.Phone,
                Email     = u.Email,
                Role      = u.Role,
            })
            .ToListAsync(ct);
 
        return new Page<UserDto>
        {
            Items    = users,
            Total    = total,
            Page     = filter.Page,
            PageSize = filter.PageSize,
        };
    }

    // PROFILE
    public async Task<UserDto> Profile(Guid userId, CancellationToken ct)
    {
        var user = await userRepo.FirstOrDefaultAsync(userId, ct)
            ?? throw new NotFoundException("Account not found");
 
        return new UserDto
        {
            Id     = user.Id,
            Avatar = user.Avatar,
            Name   = user.Name,
            Phone  = user.Phone,
            Email  = user.Email,
            Role   = user.Role,
        };
    }

    // BY ID
    public async Task<UserDto> ByIdAsync(Guid userId, CancellationToken ct)
    {
        var user = await userRepo.FirstOrDefaultAsync(userId, ct)
            ?? throw new NotFoundException("Account not found");
 
        return new UserDto
        {
            Id     = user.Id,
            Avatar = user.Avatar,
            Name   = user.Name,
            Phone  = user.Phone,
            Email  = user.Email,
            Role   = user.Role,
        };
    }

    public async Task<UserDTO> CreateAsync(CreateUserDtO dto, CancellationToken ct)
    {
        var exist = await userRepo.AnyAsync(dto.Phone, dto.Email, ct);
        if (exist)
            throw new ConflictException("Phone or email created");

        var hashPass = BCrypt.Net.BCrypt.HashPassword(dto.Password);
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Phone = dto.Phone,
            Email = dto.Email,
            Password = hashPass,
            Role = Role.USER,
        };

        await userRepo.Add(user, ct);

        try
        {
            await userRepo.SaveChangesAsync(ct);
        }
        catch (DbUpdateException e)
        {
            logger.LogWarning(e, $"Duplicate user for {user.Phone} or {user.Email}");
            throw new ConflictException("Phone or email created");
        }

        return new UserDTO{
            Id = user.Id,
            Name = user.Name,
            Phone = user.Phone,
            Email = user.Email,
            Role = user.Role,
        };
    }

    // UPLOAD AVATAR
    public async Task<string> UploadAvatarAsync(IFormFile file, Guid userId, CancellationToken ct)
    {
        var user = await userRepo.FirstOrDefaultAsync(userId, ct)
            ?? throw new NotFoundException("Account not found");
 
        // Validate file
        var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp" };
        if (!allowedTypes.Contains(file.ContentType))
            throw new BadRequestException("Chỉ hỗ trợ file JPG, PNG, WEBP");
 
        const long maxSize = 5 * 1024 * 1024;  // 5MB
        if (file.Length > maxSize)
            throw new BadRequestException("File không được vượt quá 5MB");
 
        // Xóa avatar cũ nếu có
        if (!string.IsNullOrEmpty(user.Avatar))
            await fileService.DeleteAsync(user.Avatar, ct);
 
        // Upload avatar mới
        var path = await fileService.UploadAsync(file, $"avatars/{userId}", ct);
 
        user.Avatar = path;
        await userRepo.SaveChangesAsync(ct);
 
        return path;
    }

    // UPDATE USER
    public async Task<UserDto> UpdateAsync(UpdateUserDto dto, Guid userId, , CancellationToken ct)
    {
        var user = await userRepo.FirstOrDefaultAsync(userId, ct);
        if (user is null)
            throw new NotFoundException("Account not found");

        user.Name = dto.Name;
        user.Phone = dto.Phone;
        user.Email = dto.Email;

        await userRepo.SaveChangesAsync(ct);

        return new UserDto{
            Id = user.Id,
            Name = user.Name,
            Phone = user.Phone,
            Email = user.Email,
            Role = user.Role,
        };
    }

    // DELETE
    public async Task DeleteAsync(Guid userId, CancellationToken ct)
    {
        var user = await userRepo.FirstOrDefaultAsync(userId, ct)
        if (user is null)
            throw new NotFoundException("Account not found");
 
        user.IsActive = false;

        await userRepo.SaveChangesAsync(ct);
 
        logger.LogInformation($"User:{userId} deleted}");
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