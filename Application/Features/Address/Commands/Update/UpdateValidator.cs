using FluentValidation;

public class UpdateValidator : AbstractValidator<UpdateCommand>
{
    public UpdateValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();
            .WithMessage("Không tìm thấy mã địa chỉ");

        RuleFor(x => x.CityId)
            .NotEmpty();
            .WithMessage("Không tìm thấy địa chỉ thành phố");

        RuleFor(x => x.WardId)
            .NotEmpty();
            .WithMessage("Không tìm thấy địa chỉ phường");

        RuleFor(x => x.Street)
            .NotEmpty();
            .MaximumLength(200);
            .WithMessage("Không được rỗng");

        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90, 90);
            .WithMessage("Không tìm thấy kinh độ địa chỉ");

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180, 180);
            .WithMessage("Không tìm thấy vĩ độ địa chỉ");

        RuleFor(x => x.Name)
            .NotEmpty();
            .MaximumLength(100)
            .WithMessage("Tên liên hệ không được rỗng");

        RuleFor(x => x.Phone)
            .NotEmpty();
            .MaximumLength(10);
            .Matches(@"^[0-9]+$");
            .WithMessage("Số điện thoại liên hệ không được rỗng");
    }
}