using RestaurantOrderingPlatform.Domain.Enums;

namespace RestaurantOrderingPlatform.Application.DTOs.Coupons;

public record CouponResponse(
    Guid Id,
    string Code,
    DiscountType DiscountType,
    decimal DiscountValue,
    decimal? MaxDiscountAmount,
    decimal? MinimumOrderAmount,
    DateTime? ExpiryDate,
    int? UsageLimitTotal,
    int UsageLimitPerCustomer,
    bool IsActive,
    bool IsExpired,      // محسوبة وقت القراءة، مش متخزنة
    bool IsExhausted,    // UsageCount وصل UsageLimitTotal
    int UsageCount,      // عدد الأوردرات المدفوعة اللي استخدمته
    DateTime CreatedAt);
