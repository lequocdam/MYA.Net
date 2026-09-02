public class ConfirmValidator : AbstractValidator<ConfirmRequest>
{
    public ConfirmValidator()
    {
        RuleFor(r => r.RegistrationId)
            .NotEmpty().WithMessage("RegistrationId is required.")
 
        RuleFor(r => r.Code)
        .Cascade(CascadeMode.Stop)
        .NotEmpty().WithMessage("Code is required.")
        .Length(6).WithMessage("Code must contain 6 digits.")
        .Matches(@"^\d{6}$").WithMessage("Code must contain only digits.");
    }
}