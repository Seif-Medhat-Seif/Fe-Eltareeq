using FluentValidation;
using RestaurantOrderingPlatform.Application.DTOs.Restaurant;

namespace RestaurantOrderingPlatform.Application.Validators.Restaurant;

public class UpdateRestaurantRequestValidator : AbstractValidator<UpdateRestaurantRequest>
{
    public UpdateRestaurantRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.CuisineType).MaximumLength(50);
        RuleFor(x => x.LogoUrl).MaximumLength(500);
        RuleFor(x => x.CoverImageUrl).MaximumLength(500);
        RuleFor(x => x.Street).NotEmpty().MaximumLength(200);
        RuleFor(x => x.City).NotEmpty().MaximumLength(100);
        RuleFor(x => x.DeliveryFee).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MinimumOrderAmount).GreaterThanOrEqualTo(0).When(x => x.MinimumOrderAmount.HasValue);
        RuleFor(x => x.EstimatedDeliveryTimeMinutes).GreaterThan(0).When(x => x.EstimatedDeliveryTimeMinutes.HasValue);
    }
}
