public class OtpValidator : AbstractValidator<OtpDTO>
{
    public OtpValidator()
    {
        RuleFor(o => o.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Yêu cầu email")
            .EmailAddress().WithMessage("Yêu cầu phải đúng email");
 
        RuleFor(o => o.Password)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Yêu cầu otp")
            .MinimumLength(6).WithMessage("Otp phải có ít nhất 6 ký tự")
            .Matches("[0-9]").WithMessage("Otp phải là chữ số")
    }
}