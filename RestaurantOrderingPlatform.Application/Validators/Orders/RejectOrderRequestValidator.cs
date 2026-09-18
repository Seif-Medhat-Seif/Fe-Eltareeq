using FluentValidation;
using RestaurantOrderingPlatform.Application.DTOs.Order;

namespace RestaurantOrderingPlatform.Application.Validators.Orders;

public class RejectOrderRequestValidator : AbstractValidator<RejectOrderRequest>
{
    public RejectOrderRequestValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Rejection reason is required.")
            .MaximumLength(500);
    }
}
