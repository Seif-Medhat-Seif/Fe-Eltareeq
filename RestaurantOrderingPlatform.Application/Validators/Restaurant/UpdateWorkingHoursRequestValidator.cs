using FluentValidation;
using RestaurantOrderingPlatform.Application.DTOs.Restaurant;

namespace RestaurantOrderingPlatform.Application.Validators.Restaurant;

public class UpdateWorkingHoursRequestValidator : AbstractValidator<UpdateWorkingHoursRequest>
{
    public UpdateWorkingHoursRequestValidator()
    {
        RuleFor(x => x.Days)
            .Must(days => days.Count == 7)
            .WithMessage("Exactly 7 days must be provided (whole-week replace only).");

        RuleFor(x => x.Days)
            .Must(days => days.Select(d => d.DayOfWeek).Distinct().Count() == days.Count)
            .WithMessage("Each day of the week must appear exactly once.");

        RuleForEach(x => x.Days).ChildRules(day =>
        {
            day.RuleFor(d => d.DayOfWeek).IsInEnum();

            // OpenTime/CloseTime are required together when NOT closed.
            // CloseTime < OpenTime is explicitly ALLOWED (overnight hours) —
            // no comparison rule between them here on purpose.
            day.RuleFor(d => d.OpenTime)
               .NotNull()
               .When(d => !d.IsClosed)
               .WithMessage("OpenTime is required when the day is not closed.");

            day.RuleFor(d => d.CloseTime)
               .NotNull()
               .When(d => !d.IsClosed)
               .WithMessage("CloseTime is required when the day is not closed.");
        });
    }
}
