using FluentValidation;

public class CreateValidator : AbstractValidator<CreateCommand>
{
    public Validate()
    {
        RuleFor(x => x.CityId)
            .NotEmpty();
            .WithMessage("Không tìm thấy thành phố");

        RuleFor(x => x.WardId)
            .NotEmpty();
            .WithMessage("Không tìm thấy phường");

        RuleFor(x => x.Name)
            .NotEmpty();
            .WithMessage("Tên không được rống");

        RuleFor(x => x.Phone)
            .NotEmpty();
            .WithMessage("Số điện thoại không được rỗng");

        RuleFor(x => x.Street)
            .NotEmpty();
            .WithMessage("Địa chỉ không được rỗng");

        RuleFor(x => x.Latitude)
            .NotEmpty();
            .WithMessage("Không tìm thấy kinh độ");

        RuleFor(x => x.Longitude)
            .NotEmpty();
            .WithMessage("Không tìm thấy vĩ độ");
    }
}