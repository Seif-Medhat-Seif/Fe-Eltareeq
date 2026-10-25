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

/// <summary>مفيهوش Code: الكود ثابت بعد الإنشاء (الكوبون موزّع على العملاء، وتغييره بيكسر اللي معاه الكود القديم).</summary>

public record UpdateCouponRequest(
    DiscountType DiscountType,
    decimal DiscountValue,
    decimal? MaxDiscountAmount,
    decimal? MinimumOrderAmount,
    DateTime? ExpiryDate,
    int? UsageLimitTotal,
    int UsageLimitPerCustomer = 1) : ICouponTerms;

public record SetCouponActiveRequest(bool IsActive);