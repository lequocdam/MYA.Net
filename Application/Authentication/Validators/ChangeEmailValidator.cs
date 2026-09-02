public class ChangeEmailValidator : AbstractValidator<ChangeEmailRequest>
{
    public ChangeEmailValidator()
    {
        RuleFor(x => x.NewEmail)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Require email")
            .EmailAddress().WithMessage("Invalid email");

    }
}