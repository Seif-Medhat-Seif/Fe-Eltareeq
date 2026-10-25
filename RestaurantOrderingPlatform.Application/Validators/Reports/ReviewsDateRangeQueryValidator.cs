using FluentValidation;
using RestaurantOrderingPlatform.Application.DTOs.Reports;

namespace RestaurantOrderingPlatform.Application.Validators.Reports;

public class ReviewsDateRangeQueryValidator : AbstractValidator<ReviewsDateRangeQuery>
{
    public ReviewsDateRangeQueryValidator()
    {
        Include(new DateRangeRules<ReviewsDateRangeQuery>());
    }
}
