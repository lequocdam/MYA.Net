public class ResendValidator : AbstractValidator<ResendRequest>
{
    public ResendValidator()
    {
        RuleFor(x => x.RegistrationId)
            .NotEmpty().WithMessage("RegistrationId is required.")
    }
}