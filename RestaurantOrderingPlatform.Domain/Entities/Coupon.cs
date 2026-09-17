using RestaurantOrderingPlatform.Domain.Enums;

namespace RestaurantOrderingPlatform.Domain.Entities;

public class Coupon
{
    public Guid Id { get; set; }

    public Guid RestaurantId { get; set; }
    public Restaurant Restaurant { get; set; } = null!;

    // Unique per RestaurantId (not global) — enforced via composite unique
    // index in Fluent API. Always stored uppercase-normalized per Module 8.
    public string Code { get; set; } = string.Empty;

    public DiscountType DiscountType { get; set; }
    public decimal DiscountValue { get; set; }
    public decimal? MaxDiscountAmount { get; set; }   // only meaningful for Percentage
    public decimal? MinimumOrderAmount { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public int? UsageLimitTotal { get; set; }
    public int UsageLimitPerCustomer { get; set; } = 1;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }

    public ICollection<CouponUsage> Usages { get; set; } = new List<CouponUsage>();
}
