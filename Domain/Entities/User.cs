public sealed class User
{
    public Guid Id { get; private set; }
    public string? Avatar { get; private set; }
    public string? Name { get; private set; }
    public string? Email { get; private set; }
    public string? Phone { get; private set; }
    public string? HashedPassword { get; private set; }
    public Role Role { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public bool IsDeleted { get; private set; }

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
            IsDeleted = false
        }
    }

    public string UploadAvatar(string avatar)
    {
        return Avatar = avatar;
    }

    public void Update(string name)
    {
        Name = name;
    }

    public void UpdateProfile(string name)
    {
        Name = name;
    }

    public void UpdateAvatar(string avatar)
    {
        Avatar = avatar;
    }

    public void ChangeEmail(string email)
    {
        Email = email;
    }

    public void ChangePhone(string phone)
    {
        Phone = phone;
    }

    public void ChangePassword(string hashedPassword)
    {
        HashedPassword = hashedPassword;  
    }

    public void ResetPassword(string hashedPassword)
    {
        HashedPassword = hashedPassword;  
    }
}