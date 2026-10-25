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

            builder.Property(x => x.OrderNumber).IsRequired().HasMaxLength(30);
            builder.HasIndex(x => x.OrderNumber).IsUnique();

            builder.Property(x => x.AddressLabelSnapshot).IsRequired().HasMaxLength(50);
            builder.Property(x => x.AddressLineSnapshot).IsRequired().HasMaxLength(500);
            builder.Property(x => x.AddressCitySnapshot).IsRequired().HasMaxLength(100);
            builder.Property(x => x.AddressLandmarkSnapshot).HasMaxLength(200);
            builder.Property(x => x.RejectionReason).HasMaxLength(500);

            // ★ Optimistic concurrency — يمنع اتنين employees يغيّروا نفس الأوردر في نفس اللحظة
            builder.Property(x => x.RowVersion).IsRowVersion();

            // ★ مهم جدًا: enum-as-string من غير MaxLength بيبقى nvarchar(max) = مستحيل يتعمله index
            builder.Property(x => x.Status)
                   .HasConversion<string>()
                   .HasMaxLength(30);

            // ★ الـ index ده هو اللي بيخلي queue المطعم و available deliveries سريعين
            builder.HasIndex(x => new { x.RestaurantId, x.Status });
            builder.HasIndex(x => new { x.CustomerId, x.CreatedAt });
            builder.HasIndex(x => new { x.Status, x.DriverId });   // Module 7 (available deliveries)

            // AddressId بقى nullable + Restrict (الـ snapshot هو مصدر الحقيقة)
            builder.HasOne<Address>()
                   .WithMany()
                   .HasForeignKey(x => x.AddressId)
                   .IsRequired(false)
                   .OnDelete(DeleteBehavior.SetNull);

            // الضمان الحقيقي لـ BR-6: كل DriverId يظهر مرة واحدة بس بين الأوردرات النشطة.
            // ⚠️ أسماء الـ enum كنصوص هنا. لو غيّرت اسم قيمة في OrderStatus الفلتر بيبوّظ بصمت (اختبار في Phase 8).
            builder.HasIndex(x => x.DriverId)
                   .IsUnique()
                   .HasFilter("[DriverId] IS NOT NULL AND [Status] IN ('DriverAssigned','PickedUp','OnTheWay')")
                   .HasDatabaseName("UX_Orders_DriverId_ActiveDelivery");
        }
    }
}
