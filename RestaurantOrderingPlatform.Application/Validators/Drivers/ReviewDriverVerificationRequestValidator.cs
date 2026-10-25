using FluentValidation;
using RestaurantOrderingPlatform.Application.DTOs.Drivers;

namespace RestaurantOrderingPlatform.Application.Validators.Drivers;

public class ReviewDriverVerificationRequestValidator : AbstractValidator<ReviewDriverVerificationRequest>
{
    public ReviewDriverVerificationRequestValidator()
    {
        RuleFor(x => x.Decision).IsInEnum();

        RuleFor(x => x.Reason)
            .NotEmpty().When(x => x.Decision == DriverVerificationDecision.Reject)
            .WithMessage("A reason is required when rejecting a driver.")
            .MaximumLength(500);
    }
}
