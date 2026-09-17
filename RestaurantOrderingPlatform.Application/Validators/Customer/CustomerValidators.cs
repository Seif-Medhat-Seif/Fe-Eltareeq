using FluentValidation;
using RestaurantOrderingPlatform.Application.DTOs.Customer;

namespace RestaurantOrderingPlatform.Application.Validators.Customer;

public class UpdateCustomerProfileRequestValidator : AbstractValidator<UpdateCustomerProfileRequest>
{
    public UpdateCustomerProfileRequestValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(150);

        // Shape-only validation here (format/length), per Phase 6's Validator responsibility
        // boundary. Anything requiring a DB lookup (none needed here — phone uniqueness was
        // never a requirement) stays out of the validator.
        RuleFor(x => x.PhoneNumber)
            .Matches(@"^\+?[0-9]{8,15}$")
            .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber))
            .WithMessage("Phone number must be 8-15 digits, optionally prefixed with '+'.");
    }
}

public class CreateAddressRequestValidator : AbstractValidator<CreateAddressRequest>
{
    public CreateAddressRequestValidator()
    {
        RuleFor(x => x.Label).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Street).NotEmpty().MaximumLength(200);
        RuleFor(x => x.City).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Building).MaximumLength(50);
        RuleFor(x => x.Floor).MaximumLength(20);
        RuleFor(x => x.Apartment).MaximumLength(20);
        RuleFor(x => x.Landmark).MaximumLength(200);
    }
}

public class UpdateAddressRequestValidator : AbstractValidator<UpdateAddressRequest>
{
    public UpdateAddressRequestValidator()
    {
        RuleFor(x => x.Label).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Street).NotEmpty().MaximumLength(200);
        RuleFor(x => x.City).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Building).MaximumLength(50);
        RuleFor(x => x.Floor).MaximumLength(20);
        RuleFor(x => x.Apartment).MaximumLength(20);
        RuleFor(x => x.Landmark).MaximumLength(200);
    }
}
