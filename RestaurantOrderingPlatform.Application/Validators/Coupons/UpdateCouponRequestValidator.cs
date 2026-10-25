using FluentValidation;
using RestaurantOrderingPlatform.Application.DTOs.Coupons;

namespace RestaurantOrderingPlatform.Application.Validators.Coupons;

public class UpdateCouponRequestValidator : AbstractValidator<UpdateCouponRequest>
{
    public UpdateCouponRequestValidator()
    {
        Include(new CouponTermsValidator<UpdateCouponRequest>());
    }
}
