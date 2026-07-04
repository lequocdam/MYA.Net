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
            .WithMessage("Không tìm thấy tên");

        RuleFor(x => x.Phone)
            .NotEmpty();
            .WithMessage("Không tìm thấy số điện thoại");

        RuleFor(x => x.Street)
            .NotEmpty();
            .WithMessage("Không tìm thấy đường");

        RuleFor(x => x.Latitude)
            .NotEmpty();
            .WithMessage("Không tìm thấy kinh độ");

        RuleFor(x => x.Longitude)
            .NotEmpty();
            .WithMessage("Không tìm thấy vĩ độ");
    }
}