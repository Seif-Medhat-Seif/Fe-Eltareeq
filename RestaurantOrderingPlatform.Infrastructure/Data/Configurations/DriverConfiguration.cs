using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RestaurantOrderingPlatform.Domain.Entities;
using RestaurantOrderingPlatform.Infrastructure.Identity;

namespace RestaurantOrderingPlatform.Infrastructure.Data.Configurations
{
    public class DriverConfiguration : IEntityTypeConfiguration<Driver>
    {
        public void Configure(EntityTypeBuilder<Driver> builder)
        {
            builder.HasIndex(d => d.UserId).IsUnique();

            builder.HasOne<AppUser>()
                   .WithMany()
                   .HasForeignKey(d => d.UserId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.Property(d => d.NationalID).IsRequired().HasMaxLength(30);

            builder.Property(d => d.VehicleType)
                   .HasConversion<string>()
                   .HasMaxLength(20)
                   .IsRequired();

            builder.Property(d => d.VehicleImageUrl).IsRequired().HasMaxLength(500);
            builder.Property(d => d.VehiclePlateNumber).HasMaxLength(20);

            // Order.DriverId is nullable and unassigned deliveries are queried
            // via Order directly — a driver's "current delivery" is always
            // derived, never stored redundantly (avoids a second source of truth).
            builder.HasMany(d => d.Deliveries)
                   .WithOne(o => o.Driver)
                   .HasForeignKey(o => o.DriverId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
