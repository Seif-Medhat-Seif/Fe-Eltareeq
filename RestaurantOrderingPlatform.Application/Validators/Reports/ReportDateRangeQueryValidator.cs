using FluentValidation;
using RestaurantOrderingPlatform.Application.DTOs.Reports;

namespace RestaurantOrderingPlatform.Application.Validators.Reports;

public class ReportDateRangeQueryValidator : AbstractValidator<ReportDateRangeQuery>
{
    public ReportDateRangeQueryValidator()
    {
        Include(new DateRangeRules<ReportDateRangeQuery>());
        RuleFor(x => x.From).NotNull().WithMessage("'from' is required.");
        RuleFor(x => x.To).NotNull().WithMessage("'to' is required.");
    }
}
