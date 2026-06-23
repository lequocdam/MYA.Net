using FluentValidation;

public sealed class CreateOrderHistoryDtoValidator : AbstractValidator<CreateOrderHistoryDto>
{
    public CreateOrderHistoryDtoValidator()
    {
        RuleFor(x => x.OrderId)
            .NotEmpty()
            .WithMessage("OrderId is required");

        RuleFor(x => x.FromStatus)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(x => x.ToStatus)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(x => x.Trigger)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Note)
            .MaximumLength(500);
    }
}