using FluentValidation;
using RestaurantOrderingPlatform.Application.DTOs.Reviews;

namespace RestaurantOrderingPlatform.Application.Validators.Reviews;

public class ReportReviewRequestValidator : AbstractValidator<ReportReviewRequest>
{
    public ReportReviewRequestValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MinimumLength(5).MaximumLength(500);
    }
}
