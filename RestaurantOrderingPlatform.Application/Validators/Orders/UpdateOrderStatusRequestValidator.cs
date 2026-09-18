using FluentValidation;
using RestaurantOrderingPlatform.Application.DTOs.Order;
using RestaurantOrderingPlatform.Domain.Enums;

namespace RestaurantOrderingPlatform.Application.Validators.Orders;

public class UpdateOrderStatusRequestValidator : AbstractValidator<UpdateOrderStatusRequest>
{
    // الحالات الوحيدة اللي المطعم مسموح له يبعتها في الـ endpoint ده.
    private static readonly OrderStatus[] Allowed =
        { OrderStatus.Preparing, OrderStatus.Ready };

    public UpdateOrderStatusRequestValidator()
    {
        RuleFor(x => x.Status)
            .IsInEnum()
            .Must(s => Allowed.Contains(s))
            .WithMessage("Status must be one of: Preparing, Ready.");
    }
}
