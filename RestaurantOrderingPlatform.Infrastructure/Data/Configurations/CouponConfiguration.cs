using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RestaurantOrderingPlatform.Domain.Entities;

namespace RestaurantOrderingPlatform.Infrastructure.Data.Configurations
{
    public class CouponConfiguration : IEntityTypeConfiguration<Coupon>
    {
        public void Configure(EntityTypeBuilder<Coupon> builder)
        {
            // Relationship to Restaurant configured from the Restaurant side
            // (see RestaurantConfiguration.HasMany).

            builder.Property(c => c.Code).IsRequired().HasMaxLength(30);
            builder.HasIndex(c => new { c.RestaurantId, c.Code }).IsUnique();

            builder.Property(c => c.DiscountType)
                   .HasConversion<string>()
                   .HasMaxLength(20)
                   .IsRequired();

            builder.Property(c => c.DiscountValue).IsRequired();
            builder.Property(c => c.MaxDiscountAmount);
            builder.Property(c => c.MinimumOrderAmount);
            builder.Property(c => c.ExpiryDate);
            builder.Property(c => c.UsageLimitTotal);
            builder.Property(c => c.UsageLimitPerCustomer).IsRequired().HasDefaultValue(1);
            builder.Property(c => c.IsActive).IsRequired();
            builder.Property(c => c.CreatedAt).IsRequired();

            // Restrict: hard-delete blocked (409) at Service layer if any
            // CouponUsage exists — this is the DB-level backstop.
            builder.HasMany(c => c.Usages)
                   .WithOne(u => u.Coupon)
                   .HasForeignKey(u => u.CouponId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
