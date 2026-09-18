using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RestaurantOrderingPlatform.Domain.Entities;

namespace RestaurantOrderingPlatform.Infrastructure.Data.Configurations
{
    public class CouponUsageConfiguration : IEntityTypeConfiguration<CouponUsage>
    {
        public void Configure(EntityTypeBuilder<CouponUsage> builder)
        {
            // Relationship to Coupon configured from CouponConfiguration.HasMany.
            // Relationship to Customer: no collection nav on Customer for this
            // (Customer doesn't expose a CouponUsages collection), so it's
            // configured here directly.
            builder.HasOne(u => u.Customer)
                   .WithMany()
                   .HasForeignKey(u => u.CustomerId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Same reasoning for Order — no CouponUsages collection on Order.
            builder.HasOne(u => u.Order)
                   .WithMany()
                   .HasForeignKey(u => u.OrderId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.Property(u => u.UsedAt).IsRequired();

            // ★ ده اللي بيمنع استخدام نفس الكوبون مرتين على نفس الأوردر (double-redemption race)
            builder.HasIndex(x => x.OrderId).IsUnique();
            builder.HasIndex(x => new { x.CouponId, x.CustomerId });
        }
    }
}
