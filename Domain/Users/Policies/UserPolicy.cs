public sealed class UserPolicy : IUserPolicy
{
    public void CanCreate(bool exist)
    {
        if (exist)
            throw new ConflictException("Phone or email already created.");
    }

    public void CanUpdateProfile(User user)
    {
        if (currentUser.UserId != user.Id)
        {
            throw new ForbiddenException();
        }
    }
}
public sealed class UserPolicy
{
    public void CanChangeStatus(
        UserStatus currentStatus,
        UserStatus newStatus)
    {
        if (currentStatus == newStatus)
        {
            throw new BusinessValidationException(
                "User is already in this status.");
        }

        if (!IsValidTransition(currentStatus, newStatus))
        {
            throw new BusinessValidationException(
                $"Cannot change status from {currentStatus} to {newStatus}.");
        }
    }

    private static bool IsValidTransition(
        UserStatus current,
        UserStatus next)
    {
        return (current, next) switch
        {
            (UserStatus.Active, UserStatus.Inactive) => true,
            (UserStatus.Inactive, UserStatus.Active) => true,

            _ => false
        };
    }
}