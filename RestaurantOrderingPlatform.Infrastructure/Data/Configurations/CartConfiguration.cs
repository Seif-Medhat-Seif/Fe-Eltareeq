using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RestaurantOrderingPlatform.Domain.Entities;

namespace RestaurantOrderingPlatform.Infrastructure.Data.Configurations
{
    public class CartConfiguration : IEntityTypeConfiguration<Cart>
    {
        public void Configure(EntityTypeBuilder<Cart> builder)
        {
            // The Cart <-> Customer 1:1 relationship (including the unique
            // index and Cascade delete) is fully configured from the
            // Customer side (CustomerConfiguration.HasOne(c => c.Cart)).

            builder.HasOne(c => c.Restaurant)
                   .WithMany()   // Restaurant doesn't expose a Carts collection — not a useful traversal
                   .HasForeignKey(c => c.RestaurantId)
                   .OnDelete(DeleteBehavior.Restrict)
                   .IsRequired(false);

            builder.HasOne(c => c.Coupon)
                   .WithMany()
                   .HasForeignKey(c => c.CouponId)
                   .OnDelete(DeleteBehavior.Restrict)
                   .IsRequired(false);

            // Composition: a CartItem has no meaning outside its Cart.
            builder.HasMany(c => c.Items)
                   .WithOne(ci => ci.Cart)
                   .HasForeignKey(ci => ci.CartId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
