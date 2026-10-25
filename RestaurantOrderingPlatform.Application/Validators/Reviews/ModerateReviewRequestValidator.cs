using FluentValidation;
using RestaurantOrderingPlatform.Application.DTOs.Reviews;

namespace RestaurantOrderingPlatform.Application.Validators.Reviews;

public class ModerateReviewRequestValidator : AbstractValidator<ModerateReviewRequest>
{
    public ModerateReviewRequestValidator()
    {
        RuleFor(x => x.Action).IsInEnum();
    }
}
