public sealed class UserAccessPolicy(
    ICurrentUser currentUser,
    ILogger<UserAccessPolicy> logger) : IUserAccessPolicy
{
    public void CanGetUsers()
    {
        EnsurePermission(UserPermissions.Read);
    }

    public void CanCreate()
    {
        EnsurePermission(UserPermissions.Create);
    }

    public void CanChangePassword(User user, ChangePasswordRequest request)
    {
        if (!BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
        {
            throw new ValidationException("Invalid current password");
        }

        if (BCrypt.Verify(request.NewPassword, user.PasswordHash))
        {
            throw new ConflictException("New password must be different.");
        }
    }

    public void CanChangeStatus(User targetUser)
    {
        EnsurePermission(UserPermissions.ChangeStatus);

        EnsureScope(targetUser);

        EnsureNotSelfAction(targetUser);
    }

    private void EnsurePermission(string permission)
    {
        if (currentUser.IsAdmin)
            return;

        if (currentUser.HasPermission(permission))
            return;

        logger.LogWarning("User {ActorId} attempted operation {Permission} without permission.",
            currentUser.UserId,
            permission);

        throw new ForbiddenException();
    }

    private void EnsureScope(User targetUser)
    {
        // Admin đã được bypass permission/scope
        if (currentUser.IsAdmin)
            return;

        if (currentUser.HubId is null)
        {
            logger.LogWarning(
                "User {ActorId} has no Hub scope.",
                currentUser.UserId);

            throw new ForbiddenException();
        }

        if (targetUser.HubId != currentUser.HubId)
        {
            logger.LogWarning(
                "User {ActorId} attempted to access target {TargetId} outside scope.",
                currentUser.UserId,
                targetUser.Id);

            throw new ForbiddenException();
        }
    }

    private void EnsureNotSelfAction(User targetUser)
    {
        if (currentUser.IsAdmin)
            return;

        if (currentUser.UserId == targetUser.Id)
        {
            throw new BusinessValidationException(
                "Managers cannot perform this action on themselves.");
        }
    }
}