public class RegisterValidator : AbstractValidator<RegisterDTO>
{
    public RegisterValidator()
    {
        RuleFor(r => r.Name)
            .NotEmpty().WithMessage("Yêu cầu tên người dùng")

        RuleFor(r => r.Phone)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Yêu cầu số điện thoại")
            .Matches(@"^(0|\+84)[3-9]\d{8}$").WithMessage("Số điện thoại phải đúng định dạng");
 
        RuleFor(r => r.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Yêu cầu email")
            .EmailAddress().WithMessage("Email phải đúng định dạng");
 
        RuleFor(r => r.Password)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Yêu cầu mật khẩu")
            .MinimumLength(8).WithMessage("Mật khẩu phải có ít nhất 8 ký tự")
            .Matches("[a-z]").WithMessage("Mật khẩu phải có ít nhất 1 chữ thường")
            .Matches("[A-Z]").WithMessage("Mật khẩu phải có ít nhất 1 chữ hoa")
            .Matches("[0-9]").WithMessage("Mật khẩu phải có ít nhất 1 chữ số")
    }
}