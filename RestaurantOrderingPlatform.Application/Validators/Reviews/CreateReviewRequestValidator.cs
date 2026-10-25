using FluentValidation;
using RestaurantOrderingPlatform.Application.DTOs.Reviews;

namespace RestaurantOrderingPlatform.Application.Validators.Reviews;

public class CreateReviewRequestValidator : AbstractValidator<CreateReviewRequest>
{
    public CreateReviewRequestValidator()
    {
        RuleFor(x => x.Rating).InclusiveBetween(1, 5);
        RuleFor(x => x.Comment).MaximumLength(1000);
    }
}
