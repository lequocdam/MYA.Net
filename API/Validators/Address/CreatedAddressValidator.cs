public class CreatedAddressValidator : AbstractValidator<CreatedAddressDTO>
{
    public CreatedAddressValidator()
    {
        RuleFor(x => x.Name).NotNull()
            .WithMessage("Tên người nhận hoặc gửi không được trống!");

        RuleFor(x => x.Phone).NotNull()
            .WithMessage("Số điện thoại người nhận hoặc gửi không được trống!");

        RuleFor(x => x.Email).NotNull()
            .WithMessage("Email người nhận hoặc gửi không được trống!");

        RuleFor(x => x.Address).NotNull()
            .WithMessage("Địa chỉ người nhận hoặc gửi không được trống!");
    }
}
