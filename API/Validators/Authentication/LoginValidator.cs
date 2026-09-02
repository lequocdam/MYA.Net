public class LoginValidator : AbstractValidator<LoginDTO>
{
    public LoginValidator()
    {
        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Số điện thoại không rỗng")
            .Matches(@"^(0|\+84)[3-9]\d{8}$").WithMessage("Số điện thoại không hợp lệ");
 
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email không rỗng")
            .EmailAddress().WithMessage("Email không hợp lệ");
 
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Mật khẩu không rỗng")
            .MinimumLength(8).WithMessage("Mật khẩu tối thiểu 8 ký tự")
            .Matches("[A-Z]").WithMessage("Mật khẩu phải có ít nhất 1 chữ hoa")
            .Matches("[0-9]").WithMessage("Mật khẩu phải có ít nhất 1 chữ số");
    }
}