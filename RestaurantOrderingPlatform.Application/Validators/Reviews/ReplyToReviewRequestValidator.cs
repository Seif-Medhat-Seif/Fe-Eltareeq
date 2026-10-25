using FluentValidation;
using RestaurantOrderingPlatform.Application.DTOs.Reviews;

namespace RestaurantOrderingPlatform.Application.Validators.Reviews;

public class ReplyToReviewRequestValidator : AbstractValidator<ReplyToReviewRequest>
{
    public ReplyToReviewRequestValidator()
    {
        RuleFor(x => x.Reply).NotEmpty().MaximumLength(1000);
    }
}
