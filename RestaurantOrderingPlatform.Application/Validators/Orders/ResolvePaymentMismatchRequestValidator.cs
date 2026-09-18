using FluentValidation;
using RestaurantOrderingPlatform.Application.DTOs.Order;

namespace RestaurantOrderingPlatform.Application.Validators.Orders;

public class ResolvePaymentMismatchRequestValidator : AbstractValidator<ResolvePaymentMismatchRequest>
{
    public ResolvePaymentMismatchRequestValidator()
    {
        RuleFor(x => x.Resolution).IsInEnum();
        RuleFor(x => x.AdminNote).MaximumLength(500);
    }
}
