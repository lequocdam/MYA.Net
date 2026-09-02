using FluentValidation;

public class RegisterValidator : AbstractValidator<RegisterRequest>
{
    public RegisterValidator()
    {
        RuleFor(x => x.Name)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(100).WithMessage("Password must be at most 100 characters");

        RuleFor(x => x.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Email is required")
            .MaximumLength(255).WithMessage("Email must be at most 255 characters")
            .EmailAddress().WithMessage("Email format is invalid");

        RuleFor(x => x.Phone)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Phone is required")
            .MaximumLength(12).WithMessage("Phone must be at most 12 characters")
            .Matches(@"^(0|\+84)[3-9]\d{8}$").WithMessage("Phone format is invalid");

        RuleFor(x => x.Password)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters")
            .Matches("[a-z]").WithMessage("Password must be at least one lowercase letter")
            .Matches("[A-Z]").WithMessage("Password must be at least one uppercase letter")
            .Matches("[0-9]").WithMessage("Password must be at least one digit");
    }
}