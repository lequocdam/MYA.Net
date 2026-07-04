using FluentValidation;

namespace MYA.Application.Orders.Commands.Cancel;

public sealed class CancelValidator : AbstractValidator<CancelCommand>
{
    public CancelValidator()
    {
        RuleFor(x => x.OrderIds).NotEmpty().WithMessage("Phải có ít nhất 1 đơn hủy");
        RuleFor(x => x.Reason).NotEmpty().WithMessage("Không được để trống lý do hủy đơn");
    }
}