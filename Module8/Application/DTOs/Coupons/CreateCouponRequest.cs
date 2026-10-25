using RestaurantOrderingPlatform.Domain.Enums;

namespace RestaurantOrderingPlatform.Application.DTOs.Coupons;

/// <summary>ExpiryDate بصيغة ISO-8601 UTC (فيها Z). UsageLimitPerCustomer الافتراضي 1 زي الـ spec.</summary>
public record CreateCouponRequest(
    string Code,
    DiscountType DiscountType,
    decimal DiscountValue,
    decimal? MaxDiscountAmount,
    decimal? MinimumOrderAmount,
    DateTime? ExpiryDate,
    int? UsageLimitTotal,
    int UsageLimitPerCustomer = 1) : ICouponTerms;
