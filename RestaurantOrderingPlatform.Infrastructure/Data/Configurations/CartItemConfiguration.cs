using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RestaurantOrderingPlatform.Domain.Entities;

namespace RestaurantOrderingPlatform.Infrastructure.Data.Configurations;

public class CartItemConfiguration : IEntityTypeConfiguration<CartItem>
{
    public void Configure(EntityTypeBuilder<CartItem> builder)
    {
        builder.ToTable("CartItems");

        builder.HasKey(ci => ci.Id);

        builder.Property(ci => ci.Quantity)
            .IsRequired();

        // AMENDMENT (Module 5): CartItem was deliberately designed with no price
        // column (Phase 4) so GET /api/cart always recalculates live. BUT the
        // locked Phase 5 spec also requires a `priceChanged` flag per item, which
        // is mathematically impossible without SOME baseline to compare the
        // current price against. PriceAtAdd is that baseline ONLY — it is never
        // used for totals/checkout math (those always read MenuItem.Price live).
        // Precision comes from the global (18,2) convention, not set here.
        builder.Property(ci => ci.PriceAtAdd)
            .IsRequired();

        // Composition: configured from the parent (Cart) side only — Cart is the
        // one already-configured entity from Module 1, so this FK/delete-behavior
        // pairing must be added there, not duplicated here, to avoid the
        // RestaurantId1-style duplicate-FK bug from Module 1.
        // If Cart.cs's existing configuration does NOT yet have:
        //
        //   builder.HasMany(c => c.Items)
        //       .WithOne(ci => ci.Cart)
        //       .HasForeignKey(ci => ci.CartId)
        //       .OnDelete(DeleteBehavior.Cascade); // Cart is disposable state
        //
        // add it now in CartConfiguration.cs — do NOT add a mirrored
        // HasOne(ci => ci.Cart).WithMany(...) here.

        // MenuItem FK: Restrict, not Cascade. A CartItem referencing a soft-deleted
        // or later hard-edited MenuItem must not silently vanish — CartService
        // decides what to show (isAvailable=false), never the database.
        builder.HasOne(ci => ci.MenuItem)
            .WithMany()
            .HasForeignKey(ci => ci.MenuItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
