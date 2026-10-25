using FluentValidation;
using RestaurantOrderingPlatform.Application.DTOs.Reports;

namespace RestaurantOrderingPlatform.Application.Validators.Reports;

/// <summary>القواعد المشتركة: To >= From، والنطاق مش أكتر من سنة (حماية للـ aggregation).</summary>
public class DateRangeRules<T> : AbstractValidator<T> where T : IDateRangeQuery
{
    public const int MaxDays = 366;

    public DateRangeRules()
    {
        RuleFor(x => x.To!.Value)
            .GreaterThanOrEqualTo(x => x.From!.Value)
            .WithMessage("'to' must be on or after 'from'.")
            .When(x => x.From.HasValue && x.To.HasValue)
            .OverridePropertyName("To");

        RuleFor(x => x)
            .Must(x => x.To!.Value.DayNumber - x.From!.Value.DayNumber < MaxDays)
            .WithMessage($"Date range cannot exceed {MaxDays} days.")
            .When(x => x.From.HasValue && x.To.HasValue && x.To >= x.From)
            .OverridePropertyName("To");

        // بس واحد منهم → غامض. الاتنين أو ولا واحد.
        RuleFor(x => x.To).NotNull().When(x => x.From.HasValue).WithMessage("'to' is required when 'from' is provided.");
        RuleFor(x => x.From).NotNull().When(x => x.To.HasValue).WithMessage("'from' is required when 'to' is provided.");
    }
}
