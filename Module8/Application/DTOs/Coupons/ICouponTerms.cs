using RestaurantOrderingPlatform.Domain.Enums;

namespace RestaurantOrderingPlatform.Application.DTOs.Coupons;

/// <summary>شروط الكوبون المشتركة بين Create و Update، عشان قواعد التحقق تتكتب مرة واحدة.</summary>
public interface ICouponTerms
{
    DiscountType DiscountType { get; }
    decimal DiscountValue { get; }
    decimal? MaxDiscountAmount { get; }
    decimal? MinimumOrderAmount { get; }
    DateTime? ExpiryDate { get; }
    int? UsageLimitTotal { get; }
    int UsageLimitPerCustomer { get; }
}
