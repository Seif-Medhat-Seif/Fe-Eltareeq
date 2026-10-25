using FluentValidation;
using RestaurantOrderingPlatform.Application.DTOs.Drivers;

namespace RestaurantOrderingPlatform.Application.Validators.Drivers;

public class ReleaseDeliveryRequestValidator : AbstractValidator<ReleaseDeliveryRequest>
{
    public ReleaseDeliveryRequestValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("A reason is required to release a delivery.")
            .MaximumLength(500);
    }
}
