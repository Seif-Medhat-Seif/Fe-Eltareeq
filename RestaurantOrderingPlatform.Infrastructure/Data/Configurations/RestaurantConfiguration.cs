using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RestaurantOrderingPlatform.Domain.Entities;

namespace RestaurantOrderingPlatform.Infrastructure.Data.Configurations
{
    public class RestaurantConfiguration : IEntityTypeConfiguration<Restaurant>
    {
        public void Configure(EntityTypeBuilder<Restaurant> builder)
        {
            builder.HasIndex(r => r.RestaurantOwnerId).IsUnique();

            builder.HasOne(r => r.RestaurantOwner)
                   .WithOne(o => o.Restaurant)
                   .HasForeignKey<Restaurant>(r => r.RestaurantOwnerId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.Property(r => r.Name).IsRequired().HasMaxLength(150);
            builder.Property(r => r.Description).HasMaxLength(1000);
            builder.Property(r => r.CuisineType).HasMaxLength(50);
            builder.Property(r => r.LogoUrl).HasMaxLength(500);
            builder.Property(r => r.CoverImageUrl).HasMaxLength(500);
            builder.Property(r => r.Street).IsRequired().HasMaxLength(200);
            builder.Property(r => r.City).IsRequired().HasMaxLength(100);
            builder.Property(r => r.Latitude);
            builder.Property(r => r.Longitude);
            builder.Property(r => r.DeliveryFee).IsRequired();
            builder.Property(r => r.MinimumOrderAmount);
            builder.Property(r => r.EstimatedDeliveryTimeMinutes);
            builder.Property(r => r.ManuallyClosed).IsRequired();
            builder.Property(r => r.HideUnavailableItems).IsRequired();
            builder.Property(r => r.CreatedAt).IsRequired();

            // Composition / owned collections — full bidirectional navigation.
            builder.HasMany(r => r.WorkingHours)
                   .WithOne(w => w.Restaurant)
                   .HasForeignKey(w => w.RestaurantId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(r => r.MenuCategories)
                   .WithOne(mc => mc.Restaurant)
                   .HasForeignKey(mc => mc.RestaurantId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(r => r.Employees)
                   .WithOne(e => e.Restaurant)
                   .HasForeignKey(e => e.RestaurantId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(r => r.Coupons)
                   .WithOne(c => c.Restaurant)
                   .HasForeignKey(c => c.RestaurantId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(r => r.Orders)
                   .WithOne(o => o.Restaurant)
                   .HasForeignKey(o => o.RestaurantId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(r => r.Reviews)
                   .WithOne(rv => rv.Restaurant)
                   .HasForeignKey(rv => rv.RestaurantId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
