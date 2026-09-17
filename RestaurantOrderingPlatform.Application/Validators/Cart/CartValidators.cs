using FluentValidation;
using RestaurantOrderingPlatform.Application.DTOs.Cart;

namespace RestaurantOrderingPlatform.Application.Validators.Cart;

public class AddCartItemRequestValidator : AbstractValidator<AddCartItemRequest>
{
    public AddCartItemRequestValidator()
    {
        RuleFor(x => x.MenuItemId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0).LessThanOrEqualTo(50);
    }
}

public class UpdateCartItemRequestValidator : AbstractValidator<UpdateCartItemRequest>
{
    public UpdateCartItemRequestValidator()
    {
        // Quantity=0 is intentionally rejected here — removing an item is a
        // separate DELETE endpoint, not PUT-with-zero. Keeps the two operations
        // explicit instead of overloading one endpoint with two meanings.
        RuleFor(x => x.Quantity).GreaterThan(0).LessThanOrEqualTo(50);
    }
}

public class ApplyCouponRequestValidator : AbstractValidator<ApplyCouponRequest>
{
    public ApplyCouponRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(30);
    }
}
