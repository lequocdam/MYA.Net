public class CreatedOrderDTOValidator : AbstractValidator<CreatedOrderDTO>
{
    public CreatedOrderDTOValidator()
    {
        RuleFor(x => x.Sender).NotNull()
            .WithMessage("Thông tin người gửi không được trống!");

        RuleFor(x => x.Receiver).NotNull()
            .WithMessage("Thông tin người nhận không được trống!");

        RuleFor(x => x.Items).NotEmpty()
            .WithMessage("Đơn hàng phải có ít nhất 1 sản phẩm!");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.Weight).GreaterThan(0)
                .WithMessage("Cân nặng phải lớn hơn 0");
            item.RuleFor(i => i.Quantity).GreaterThan(0)
                .WithMessage("Số lượng phải lớn hơn 0");
            item.RuleFor(i => i.Name).NotEmpty().MaximumLength(200);
        });
    }
}
