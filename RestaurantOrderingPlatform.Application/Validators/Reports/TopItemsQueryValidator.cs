using FluentValidation;
using RestaurantOrderingPlatform.Application.DTOs.Reports;

namespace RestaurantOrderingPlatform.Application.Validators.Reports;

public class TopItemsQueryValidator : AbstractValidator<TopItemsQuery>
{
    public TopItemsQueryValidator()
    {
        Include(new DateRangeRules<TopItemsQuery>());
        RuleFor(x => x.From).NotNull().WithMessage("'from' is required.");
        RuleFor(x => x.To).NotNull().WithMessage("'to' is required.");
        RuleFor(x => x.Limit).InclusiveBetween(1, 50);
    }
}
