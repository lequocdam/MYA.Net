public sealed class User
{
    public Guid Id { get; private set; }
    public string Avatar { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public bool IsEmailVerified { get; private set; } = false;
    public string Phone { get; private set; } = null!;
    public bool IsPhoneVerified { get; private set; } = false;
    public string HashedPassword { get; private set; } = null!;
    public Role Role { get; private set; }
    public UserStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public bool IsLocked { get; private set; }
    public bool IsDeleted { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public static User Create(
        string name,
        string email,
        string phone,
        string hashedPassword)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Name = name,
            Email = email,
            Phone = phone,
            HashedPassword = hashedPassword,
            Role = Role.User,
            CreatedAt = DateTime.UtcNow,
            IsLocked = false,
            IsDeleted = false
        }
    }

    public void VerifyEmail()
    {
        if (IsEmailVerified)
            throw new DomainException("Email is verified.");

        IsEmailVerified = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void VerifyPhone()
    {
        if (IsPhoneVerified)
            throw new DomainException("Phone is verified.");

        IsPhoneVerified = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Update(
        string name,
        string email,
        string phone)
    {
        Name = name;
        Email = email;
        Phone = phone;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateProfile(string name)
    {
        Name = name;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ChangeEmail(string email)
    {
        if (Email == email)
            throw new DomainException("Email is used.");

        Email = email;
        IsEmailVerified = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ChangePhone(string phone)
    {
        if (Phone == phone)
            throw new DomainException("Email is used.");

        Phone = phone;
        IsPhoneVerified = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ChangePassword(string hashedPassword)
    {
        HashedPassword = hashedPassword;  
        UpdatedAt = DateTime.UtcNow;
    }

    public void ChangeStatus(UserStatus status)
    {
        if (IsLocked)
            throw new DomainException("User is locked.");

        if (IsDeleted)
            throw new DomainException("User is deleted.");

        if (Status == status)
            throw new DomainException($"User is {Status}.");

        Status = status;  
        UpdatedAt = DateTime.UtcNow;
    }

    public void Unlock()
    {
        if (!IsLocked)
            throw new DomainException("User is not locked.");

        IsLocked = false;  
        UpdatedAt = DateTime.UtcNow;
    }

    public void Delete()
    {
        if (IsDeleted)
            throw new DomainException("User is deleted.");

        IsDeleted = true;  
        UpdatedAt = DateTime.UtcNow;
    }
}