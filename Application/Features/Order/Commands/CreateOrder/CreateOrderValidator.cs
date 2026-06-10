using FluentValidation;

public class CreateOrderValidator
    : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderValidator()
    {
        RuleFor(x => x.Dto.Sender)
            .NotNull();

        RuleFor(x => x.Dto.Receiver)
            .NotNull();

        RuleFor(x => x.Dto.Items)
            .NotEmpty();

        RuleForEach(x => x.Dto.Items)
            .ChildRules(item =>
            {
                item.RuleFor(i => i.Quantity)
                    .GreaterThan(0);

                item.RuleFor(i => i.Weight)
                    .GreaterThan(0);
            });
    }
}