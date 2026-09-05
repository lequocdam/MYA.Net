public class Registration
{
    public Guid Id { get; private set; }
    public string? Name { get; private set; }
    public string? Email { get; private set; }
    public string? Phone { get; private set; }
    public string? HashedPassword { get; private set; }
    public RegistrationStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Registration() {}

    public static Registration Create(
        string name,
        string email,
        string phone,
        string hashedPassword)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Name is required.");

        if (string.IsNullOrWhiteSpace(hashedPassword))
            throw new DomainException("Hashed password is required.");

        return new Registration
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Email = email,
            Phone = phone,
            HashedPassword = hashedPassword.Trim(),
            Status = RegistrationStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            ExpiredAt = DateTime.UtcNow.Add(PendingChangeExpiry)
        }
    }

    public void MarkConfirmed()
    {
        if (Status != RegistrationStatus.Pending)
            throw new DomainException("Registration is not pending.");

        Status = RegistrationStatus.Confirmed;
        ConfirmedAt = DateTime.UtcNow;
    }

    public void MarkExpired()
    {
        if (Status != UserChangeStatus.Pending)
            throw new DomainException("Registration is not pending.");

        Status = UserChangeStatus.Expired;
        ExpiredAt = DateTime.UtcNow;
    }
}