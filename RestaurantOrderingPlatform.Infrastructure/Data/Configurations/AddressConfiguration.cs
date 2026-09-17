using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RestaurantOrderingPlatform.Domain.Entities;

namespace RestaurantOrderingPlatform.Infrastructure.Data.Configurations;

public class AddressConfiguration : IEntityTypeConfiguration<Address>
{
    public void Configure(EntityTypeBuilder<Address> builder)
    {
        builder.ToTable("Addresses");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Label).IsRequired().HasMaxLength(50);
        builder.Property(a => a.Street).IsRequired().HasMaxLength(200);
        builder.Property(a => a.City).IsRequired().HasMaxLength(100);
        builder.Property(a => a.Building).HasMaxLength(50);
        builder.Property(a => a.Floor).HasMaxLength(20);
        builder.Property(a => a.Apartment).HasMaxLength(20);
        builder.Property(a => a.Landmark).HasMaxLength(200);

        builder.Property(a => a.CreatedAt)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        // Many per Customer. Restrict, not Cascade: an Address can be referenced by
        // Order.AddressId (historical delivery address), so it must never disappear
        // silently via a Customer-level cascade — same reasoning family as the
        // Customer/RestaurantOwner/Driver -> AppUser Restrict decision from Module 1.
        builder.HasOne(a => a.Customer)
            .WithMany(c => c.Addresses)
            .HasForeignKey(a => a.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        // Same technique as RestaurantEmployee's BR-2 index: a customer can have at
        // most ONE address with IsDefault=1 at any time (zero is fine — a brand new
        // customer with no addresses yet). This is a real DB-level guarantee, not
        // just a Service-layer promise; CustomerService still clears-then-sets
        // inside a transaction purely to return a clean error instead of a raw
        // constraint violation if that invariant is ever violated by a bug.
        builder.HasIndex(a => a.CustomerId)
            .HasFilter("[IsDefault] = 1")
            .IsUnique();
    }
}
