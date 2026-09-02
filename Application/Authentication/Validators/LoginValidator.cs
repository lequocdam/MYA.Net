public class LoginValidator : AbstractValidator<LoginRequest>
{
    public LoginValidator()
    {
        RuleFor(x => x.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Require email")
            .EmailAddress().WithMessage("Invalid email");
 
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Require password")
    }
}