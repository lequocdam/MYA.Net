public class Registration
{
    public Guid Id { get; private set; }

    public string Name { get; private set; }

    public string Email { get; private set; }

    public string Phone { get; private set; }

    public string PasswordHash { get; private set; }

    public DateTime CreatedAt { get; private set; }

    private Registration()
    {
        Name = string.Empty;
        Email = string.Empty;
        Phone = string.Empty;
        PasswordHash = string.Empty;
    }

    private Registration(
        string name,
        string email,
        string phone,
        string passwordHash)
    {
        Id = Guid.NewGuid();
        Name = name;
        Email = email;
        Phone = phone;
        PasswordHash = passwordHash;
        CreatedAt = DateTime.UtcNow;
    }

    public static Registration Create(
        string name,
        string email,
        string phone,
        string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Name is required.");

        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("Email is required.");

        if (string.IsNullOrWhiteSpace(phone))
            throw new DomainException("Phone is required.");

        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new DomainException("Hashed password is required.");

        return new Registration(
            name,
            email,
            phone,
            passwordHash);
    }

    public void Verify()
    {
        if(Status != RegistrationStatus.Pending)
            throw new DomainException(
                "Registration cannot verify");


        Status = RegistrationStatus.Verified;
    }


    public void IncreaseOtpAttempt()
    {
        OtpAttempts++;

        if(OtpAttempts > 5)
            throw new DomainException(
                "OTP attempt exceeded");
    }


    public bool IsExpired()
    {
        return DateTime.UtcNow > ExpiredAt;
    }


    public void Cancel()
    {
        Status = RegistrationStatus.Cancelled;
    }
}