using FluentValidation;
using RestaurantOrderingPlatform.Application.DTOs.Drivers;
using RestaurantOrderingPlatform.Domain.Enums;

namespace RestaurantOrderingPlatform.Application.Validators.Drivers;

public class UpdateDriverProfileRequestValidator : AbstractValidator<UpdateDriverProfileRequest>
{
    public UpdateDriverProfileRequestValidator()
    {
        // الرقم القومي المصري 14 رقم (افتراض موثّق: سوق مصر بس في V1).
        RuleFor(x => x.NationalId)
            .NotEmpty()
            .Matches(@"^\d{14}$").WithMessage("National ID must be exactly 14 digits.");

        RuleFor(x => x.VehicleType).IsInEnum();

        // الدراجة العادية من غير لوحة. باقي المركبات لازم لوحة.
        RuleFor(x => x.VehiclePlateNumber)
            .NotEmpty().When(x => x.VehicleType != VehicleType.Bike)
            .WithMessage("Plate number is required for this vehicle type.")
            .MaximumLength(20);

        // رفع الصور (Azure Blob) لسه مش متنفذ. بنقبل رابط https بس.
        RuleFor(x => x.VehicleImageUrl)
            .NotEmpty()
            .MaximumLength(500)
            .Must(u => Uri.TryCreate(u, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps)
            .WithMessage("Vehicle image URL must be a valid https URL.");
    }
}
