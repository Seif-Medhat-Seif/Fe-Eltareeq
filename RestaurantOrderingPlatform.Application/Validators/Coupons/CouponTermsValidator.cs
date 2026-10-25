using FluentValidation;
using RestaurantOrderingPlatform.Application.DTOs.Coupons;
using RestaurantOrderingPlatform.Domain.Enums;

namespace RestaurantOrderingPlatform.Application.Validators.Coupons;

/// <summary>القواعد المشتركة. بتتضمّن جوه Create و Update عن طريق Include().</summary>
public class CouponTermsValidator<T> : AbstractValidator<T> where T : ICouponTerms
{
    // decimal(18,2) في الداتابيز: أي كسور زيادة كانت هتتقرّب بصمت.
    private static bool TwoDecimals(decimal v) => decimal.Round(v, 2) == v;

    public CouponTermsValidator()
    {
        RuleFor(x => x.DiscountType).IsInEnum();

        RuleFor(x => x.DiscountValue)
            .GreaterThan(0)
            .Must(TwoDecimals).WithMessage("DiscountValue allows at most 2 decimal places.");

        RuleFor(x => x.DiscountValue)
            .LessThanOrEqualTo(100).WithMessage("A percentage discount cannot exceed 100.")
            .When(x => x.DiscountType == DiscountType.Percentage);

        RuleFor(x => x.MaxDiscountAmount!.Value)
            .GreaterThan(0)
            .Must(TwoDecimals).WithMessage("MaxDiscountAmount allows at most 2 decimal places.")
            .When(x => x.MaxDiscountAmount.HasValue)
            .OverridePropertyName(nameof(ICouponTerms.MaxDiscountAmount));

        // قاعدة الـ spec: MaxDiscountAmount مع Percentage بس (400 لو مع FixedAmount).
        RuleFor(x => x.MaxDiscountAmount)
            .Null().WithMessage("MaxDiscountAmount is only valid with Percentage discounts.")
            .When(x => x.DiscountType == DiscountType.FixedAmount);

        RuleFor(x => x.MinimumOrderAmount!.Value)
            .GreaterThan(0)
            .Must(TwoDecimals).WithMessage("MinimumOrderAmount allows at most 2 decimal places.")
            .When(x => x.MinimumOrderAmount.HasValue)
            .OverridePropertyName(nameof(ICouponTerms.MinimumOrderAmount));

        RuleFor(x => x.UsageLimitTotal!.Value)
            .GreaterThan(0)
            .When(x => x.UsageLimitTotal.HasValue)
            .OverridePropertyName(nameof(ICouponTerms.UsageLimitTotal));

        RuleFor(x => x.UsageLimitPerCustomer).GreaterThanOrEqualTo(1);
    }
}
