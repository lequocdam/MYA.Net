using FluentValidation;

public class CreateAddressValidator
    : AbstractValidator<CreateAddressDto>
{
    public CreateAddressValidator()
    {
        RuleFor(a => a.Name)
            .NotEmpty()
            .MaximumLength(50);
            .WithMessage("Name is required");

        RuleFor(a => a.Phone)
            .NotEmpty()
            .MaximumLength(10);
            .WithMessage("Phone is required");

        RuleFor(a => a.Street)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(a => a.WardId)
            .NotEmpty();

        RuleFor(a => a.CityId)
            .NotEmpty();

        RuleFor(a => a.Latitude)
            .InclusiveBetween(-90, 90);

        RuleFor(a => a.Longitude)
            .InclusiveBetween(-180, 180);
    }
}