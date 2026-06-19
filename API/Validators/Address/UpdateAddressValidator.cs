using FluentValidation;

public class UpdateAddressValidator
    : AbstractValidator<UpdateAddressDto>
{
    public UpdateAddressValidator()
    {
        RuleFor(a => a.Name)
            .NotEmpty()
            .MaximumLength(50)
            .WithMessage("Name is required");

        RuleFor(a => a.Phone)
            .NotEmpty()
            .MaximumLength(10)
            .WithMessage("Phone is required");

        RuleFor(a => a.Street)
            .NotEmpty()
            .MaximumLength(200);
            .WithMessage("Street is required");

        RuleFor(a => a.WardId)
            .NotEmpty()
            .WithMessage("Ward is required");

        RuleFor(a => a.CityId)
            .NotEmpty()
            .WithMessage("City is required");

        RuleFor(a => a.Latitude)
            .InclusiveBetween(-90, 90)
            .WithMessage("Latitude is invalid.");

        RuleFor(a => a.Longitude)
            .InclusiveBetween(-180, 180)
            .WithMessage("Longitude is invalid.");
    }
}