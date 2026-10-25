using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RestaurantOrderingPlatform.Domain.Entities;

namespace RestaurantOrderingPlatform.Infrastructure.Data.Configurations
{
    public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
    {
        public void Configure(EntityTypeBuilder<OrderItem> builder)
        {
            // Relationship to Order configured from the Order side
            // (composition — see OrderConfiguration.HasMany).
            // Relationship to MenuItem configured from the MenuItem side
            // (see MenuItemConfiguration.HasMany).

            // Snapshot fields — the source of truth for display. Menu
            // price/name edits after this point must NEVER retroactively
            // alter these (Phase 3 domain design principle).
            builder.Property(oi => oi.ItemNameSnapshot).IsRequired().HasMaxLength(150);
            builder.Property(oi => oi.ItemPriceSnapshot).IsRequired();

            builder.Property(oi => oi.Quantity).IsRequired();
            builder.Property(oi => oi.LineTotal).IsRequired();
            builder.HasIndex(i => i.OrderId);
        }
    }
}
