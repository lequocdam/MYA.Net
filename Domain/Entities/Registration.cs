public class Registration
{
    public Guid Id { get; private set; }
    public string? Name { get; private set; }
    public string? Email { get; private set; }
    public string? Phone { get; private set; }
    public string? HashedPassword { get; private set; }
    public RegistrationStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Registration(){}

    public static Registration Create(
        string name,
        string email,
        string phone,
        string hashedPassword)
    {
        return new Registration
        {
            Id = Guid.NewGuid();
            Name = name;
            Email = email;
            Phone = phone;
            HashedPassword = hashedPassword;
            Status = RegistrationStatus.Pending;
            CreatedAt = DateTime.UtcNow;
        }
    }

    public void MarkConfirmed()
    {
        if (!IsPending())
            throw new DomainException("Registration is not pending.");

        Status = RegistrationStatus.Confirmed;
    }

    public bool IsPending()
    {
        return Status == RegistrationStatus.Pending;
    }
}