public class UserService(
    IUserRepository userRepository,
    IFileService fileService,
    ILogger<UserService> logger) : IUserService
{

    // GET ALL
    public async Task<Page<UserDto>> GetAllAsync(UserFilterDto filter, CancellationToken ct)
    {
        var query = userRepository.Query();

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

    // GET DETAIL
    public async Task<UserDto> GetDetailAsync(Guid id, CancellationToken ct)
    {
        var user = await userRepository.SelectByIdAsync(id, ct);
        if (user is null)
            throw new NotFoundException("Account not found");

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

    // GET PROFILE
    public async Task<UserDto> GetDetailAsync(Guid userId, CancellationToken ct)
    {
        var user = await userRepository.SelectByIdAsync(userId, ct);
        if (user is null)
            throw new NotFoundException("Account not found");
 
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

    // CREATE
    public async Task<UserDTO> CreateAsync(CreateUserDtO dto, CancellationToken ct)
    {
        var exists = await userRepository.AnyAsync(dto.Phone, dto.Email, ct);
        if (exists)
            throw new ConflictException("Phone or email created");

        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(dto.Password);
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Phone = dto.Phone,
            Email = dto.Email,
            Password = hashedPassword,
            Role = dto.Role,
        };

        await userRepository.Add(user, ct);

        try
        {
            await userRepo.SaveChangesAsync(ct);
        }
        catch (DbUpdateException e)
        {
            logger.LogWarning($"Duplicate user for {user.Phone} or {user.Email}", e);
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

    public async Task<UserDto> UpdateAsync(UpdateUserDto dto, Guid id, CancellationToken ct)
    {
        var user = await userRepository.SelectByIdAsync(id, ct);
        if (user is null)
            throw new NotFoundException("Account not found");

        var exists = await userRepository.AnyAsync(dto.Phone, dto.Email, ct);
        if (exists)
            throw new ConflictException("Phone or email created");

        user.Name = dto.Name;
        user.Phone = dto.Phone;
        user.Email = dto.Email;
 
        try
        {
            await userRepository.SaveChangesAsync(ct);
        }
        catch (DbUpdateException e)
        {
            logger.LogWarning(e, "Phone or email duplicated", user.Phone, user.Email);
            throw new ConflictException("Phone or email created");
        }

        logger.LogInformation("Account {Id} updated", id);
 
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

    // ACTIVATE 
    public async Task ActivateAsync(Guid id, CancellationToken ct)
    {
        var user = await userRepository.SelectByIdAsync(id, ct);
        if (user is null)
            throw new NotFoundException("Account not found");

        if (user.IsActive)
            throw new BadRequestException("Account deleted");
 
        user.IsActive = true;

        await userRepository.SaveChangesAsync(ct);
        logger.LogInformation("Account {Id} activated", id);
    }

    public async Task<UserDto> UpdateProfileAsync(UpdateProfileDto dto, Guid userId, CancellationToken ct)
    {
        var user = await userRepository.SelectByIdAsync(userId, ct);
        if (user is null)
            throw new NotFoundException("Account not found");

        user.Name = dto.Name;
 
        await userRepo.SaveChangesAsync(ct);
        logger.LogInformation("User {Id} updated", userId);
 
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

    // UPLOAD AVATAR
    public async Task<string> UploadAvatarAsync(IFormFile file, Guid userId, CancellationToken ct)
    {
        var user = await userRepo.FirstOrDefaultAsync(userId, ct)
            ?? throw new NotFoundException("Account not found");
 
        var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp" };
        if (!allowedTypes.Contains(file.ContentType))
            throw new BadRequestException("Chỉ hỗ trợ file JPG, PNG, WEBP");
 
        const long maxSize = 5 * 1024 * 1024;
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

    public async Task ChangePasswordAsync(ChangePasswordDto dto, Guid userId, CancellationToken ct)
    {
        var user = await userRepository.SelectByIdAsync(userId, ct);
        if (user is null)
            throw new NotFoundException("Account not found");
 
        if (!BCrypt.Net.BCrypt.Verify(dto.OldPassword, user.Password))
            throw new BadRequestException("Current password is incorrect");
 
        if (BCrypt.Net.BCrypt.Verify(dto.NewPassword, user.Password))
            throw new BadRequestException("New password must be different from current password");
 
        user.Password = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);

        await userRepository.SaveChangesAsync(ct);
        logger.LogInformation("Account {Id} updated password", userId);
    }
    
    // DELETE
    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var user = await userRepository.SelectByIdAsync(id, ct);
        if (user is null)
            throw new NotFoundException("Account not found");

        if (!user.IsActive)
            throw new BadRequestException("Account deleted");
 
        user.IsActive = false;

        await userRepository.SaveChangesAsync(ct);
        logger.LogInformation("Account {Id} deleted}", id);
    }
}