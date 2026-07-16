public sealed class UserPolicy : IUserPolicy
{
    public void CanRegister(bool exists)
    {
        if (exists)
            throw new ConflictException("Phone or email are registered.");
    }
}