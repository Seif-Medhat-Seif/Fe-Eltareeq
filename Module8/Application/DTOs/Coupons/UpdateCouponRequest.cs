using RestaurantOrderingPlatform.Domain.Enums;

namespace RestaurantOrderingPlatform.Application.DTOs.Coupons;

/// <summary>مفيهوش Code: الكود ثابت بعد الإنشاء (الكوبون موزّع على العملاء، وتغييره بيكسر اللي معاه الكود القديم).</summary>
public record UpdateCouponRequest(
    DiscountType DiscountType,
    decimal DiscountValue,
    decimal? MaxDiscountAmount,
    decimal? MinimumOrderAmount,
    DateTime? ExpiryDate,
    int? UsageLimitTotal,
    int UsageLimitPerCustomer = 1) : ICouponTerms;
