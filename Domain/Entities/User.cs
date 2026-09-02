public class User
{
    public Guid Id { get; private set; }
    
    public string? Name { get; private set; }

    public string? Email { get; private set; }

    public string? Phone { get; private set; }

    public string? HashedPassword { get; private set; }

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
            Id = Guid.NewGuid();
            Name = name;
            Email = email;
            Phone = phone;
            HashedPassword = hashedPassword;
            CreatedAt = DateTime.UtcNow;
            IsDeleted = false;
        }
    }

    public void Update(
        string name,
        string email,
        string phone)
    {
        EnsureNotDeleted();

        Name = NormalizeRequired(name, nameof(name));

        Touch();
    }


    public void UpdateProfile(string name)
    {
        EnsureNotDeleted();

        Name = NormalizeRequired(name, nameof(name));

        Touch();
    }

    public void ChangeEmail(string email)
    {
        EnsureNotDeleted();

        Email = NormalizeRequired(
            email,
            nameof(email));

        Touch();
    }

    public void ChangePassword(string passwordHash)
    {
        EnsureNotDeleted();

        PasswordHash = passwordHash;
        
        Touch();
    }

    public void ResetPassword(string passwordHash)
    {
        EnsureNotDeleted();

        PasswordHash = NormalizeRequired(
            passwordHash,
            nameof(passwordHash));

        PasswordChangedAt = DateTime.UtcNow;

        Touch();
    }

    private void EnsureNotDeleted()
    {
        if (IsDeleted) throw new DomainException("User alredy deleted.");
    }
}