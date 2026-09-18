using FluentValidation;
using RestaurantOrderingPlatform.Application.DTOs.Order;

namespace RestaurantOrderingPlatform.Application.Validators.Orders;

public class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
{
    public CreateOrderRequestValidator()
    {
        RuleFor(x => x.AddressId)
            .NotEmpty().WithMessage("Delivery address is required.");

        RuleFor(x => x.Notes)
            .MaximumLength(500);
        // ملاحظة: "هل العنوان ده بتاع العميل ده؟" مش هنا — ده DB lookup
        // ودي مسؤولية الـ Service حسب Phase 6 (الـ Validator شكل بس).
    }
}
