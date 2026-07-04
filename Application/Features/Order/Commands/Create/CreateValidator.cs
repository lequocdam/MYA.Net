using FluentValidation;

public class CreateValidator : AbstractValidator<CreateCommand>
{
    public Validate()
    {
        RuleFor(x => x.ServiceId)
            .NotEmpty();
            .WithMessage("Khong tìm thấy dịch vụ giao hàng");

        RuleFor(x => x.FromAddressId)
            .NotEmpty();
            .WithMessage("Không tìm thấy địa chỉ lấy hàng");

        RuleFor(x => x.ToAddressId)
            .NotEmpty();
            .WithMessage("Không tìm thấy địa chỉ nhận hàng");

        RuleFor(x => x.Items)
            .NotEmpty();
            .WithMessage("Danh sách sản phẩm không được trống");

        RuleForEach(x => x.Items)
            .ChildRules(item =>
            {
                item.RuleFor(i => i.Name)
                    .NotEmpty();
                    .WithMessage("Tên sản phẩm không được trống");

                item.RuleFor(i => i.Quantity)
                    .GreaterThan(0);
                    .WithMessage("Số lượng sản phẩm không được nhỏ hơn 1");

                item.RuleFor(i => i.Weight)
                    .GreaterThan(0);
                    .WithMessage("Trọng lượng sản phẩm không được nhỏ hơn 1");
                
                item.RuleFor(i => i.Length)
                    .GreaterThan(0);
                    .WithMessage("Chiều dài sản phẩm không được nhỏ hơn 1");

                item.RuleFor(i => i.Width)
                    .GreaterThan(0);
                    .WithMessage("Chiều rộng sản phẩm không được nhỏ hơn 1");

                item.RuleFor(i => i.Height)
                    .GreaterThan(0);
                    .WithMessage("Chiều cao sản phẩm không được nhỏ hơn 1");
            });
    }
}