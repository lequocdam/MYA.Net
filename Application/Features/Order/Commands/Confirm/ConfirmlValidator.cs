using FluentValidation;

namespace MYA.Application.Orders.Commands.Confirm;

public sealed class ConfirmValidator : AbstractValidator<ConfirmCommand>
{
    public Validator()
    {
        RuleFor(x => x.OrderIds).NotEmpty().WithMessage("Danh sách xác nhận đơn hàng không được rỗng");

    }
}