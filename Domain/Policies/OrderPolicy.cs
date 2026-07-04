using MYA.Application.Common.Exceptions;

namespace MYA.Application.Orders.Policies;

public static class OrderPolicy
{
    public static void Validate(
        CurrentUser currentUser,
        Address fromAddress,
        Address toAddress)
    {
        if (fromAddress.UserId != currentUser.Id)
            throw new ForbiddenException(
                "From address does not belong to current user.");

        if (toAddress.UserId != currentUser.Id)
            throw new ForbiddenException(
                "To address does not belong to current user.");
    }
}