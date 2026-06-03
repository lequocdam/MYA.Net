public class RefreshValidator : AbstractValidator<RefreshDTO>
{
    public RefreshValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Không rỗng")
    }
}