using FluentValidation;
using RestaurantOrderingPlatform.Application.DTOs.Drivers;
using RestaurantOrderingPlatform.Domain.Enums;

namespace RestaurantOrderingPlatform.Application.Validators.Drivers;

public class UpdateDeliveryStatusRequestValidator : AbstractValidator<UpdateDeliveryStatusRequest>
{
    // DriverAssigned بتحصل من accept بس.
    private static readonly OrderStatus[] Allowed =
        { OrderStatus.PickedUp, OrderStatus.OnTheWay, OrderStatus.Delivered };

    public UpdateDeliveryStatusRequestValidator()
    {
        RuleFor(x => x.Status)
            .IsInEnum()
            .Must(s => Allowed.Contains(s))
            .WithMessage("Status must be one of: PickedUp, OnTheWay, Delivered.");
        // "الانتقال ده مسموح من الحالة الحالية؟" محتاج داتابيز → دور الـ Service + OrderStateMachine.
    }
}
