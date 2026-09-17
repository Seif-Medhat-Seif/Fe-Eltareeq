using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RestaurantOrderingPlatform.Domain.Entities;

namespace RestaurantOrderingPlatform.Infrastructure.Data.Configurations
{
    public class OrderConfiguration : IEntityTypeConfiguration<Order>
    {
        public void Configure(EntityTypeBuilder<Order> builder)
        {
            // Customer, Restaurant relationships configured from their
            // respective sides (HasMany on Customer/Restaurant).

            builder.HasOne(o => o.Address)
                   .WithMany()   // Address doesn't expose an Orders collection
                   .HasForeignKey(o => o.AddressId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Driver relationship configured from the Driver side
            // (DriverConfiguration.HasMany(d => d.Deliveries)).

            builder.HasOne(o => o.Coupon)
                   .WithMany()
                   .HasForeignKey(o => o.CouponId)
                   .OnDelete(DeleteBehavior.Restrict)
                   .IsRequired(false);

            builder.Property(o => o.Status)
                   .HasConversion<string>()
                   .HasMaxLength(30)
                   .IsRequired();

            builder.Property(o => o.Subtotal).IsRequired();
            builder.Property(o => o.DeliveryFee).IsRequired();
            builder.Property(o => o.DiscountAmount).IsRequired();
            builder.Property(o => o.Total).IsRequired();
            builder.Property(o => o.CreatedAt).IsRequired();

            builder.HasIndex(o => o.CustomerId);
            builder.HasIndex(o => new { o.RestaurantId, o.Status });

            // Composition: OrderItem/OrderStatusHistory/Payment have no
            // independent lifecycle outside their Order.
            builder.HasMany(o => o.Items)
                   .WithOne(oi => oi.Order)
                   .HasForeignKey(oi => oi.OrderId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(o => o.StatusHistory)
                   .WithOne(h => h.Order)
                   .HasForeignKey(h => h.OrderId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(o => o.Payments)
                   .WithOne(p => p.Order)
                   .HasForeignKey(p => p.OrderId)
                   .OnDelete(DeleteBehavior.Cascade);

            // 1:1, unique — one review per order (BR-8).
            builder.HasOne(o => o.Review)
                   .WithOne(r => r.Order)
                   .HasForeignKey<Review>(r => r.OrderId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
