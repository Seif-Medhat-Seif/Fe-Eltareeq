using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RestaurantOrderingPlatform.Domain.Entities;
using RestaurantOrderingPlatform.Infrastructure.Identity;

namespace RestaurantOrderingPlatform.Infrastructure.Data.Configurations
{
    public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
    {
        public void Configure(EntityTypeBuilder<Customer> builder)
        {
            builder.HasIndex(c => c.UserId).IsUnique();

            // One-directional: AppUser lives in Infrastructure, Domain must
            // stay blind to it — no navigation property, FK only.
            builder.HasOne<AppUser>()
                   .WithMany()
                   .HasForeignKey(c => c.UserId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Domain-to-Domain: full bidirectional navigation.
            builder.HasMany(c => c.Addresses)
                   .WithOne(a => a.Customer)
                   .HasForeignKey(a => a.CustomerId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(c => c.Cart)
                   .WithOne(cart => cart.Customer)
                   .HasForeignKey<Cart>(cart => cart.CustomerId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(c => c.Orders)
                   .WithOne(o => o.Customer)
                   .HasForeignKey(o => o.CustomerId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(c => c.Reviews)
                   .WithOne(r => r.Customer)
                   .HasForeignKey(r => r.CustomerId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(c => c.RestaurantEmployeeLinks)
                   .WithOne(e => e.Customer)
                   .HasForeignKey(e => e.CustomerId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
