using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RestaurantOrderingPlatform.Domain.Entities;

namespace RestaurantOrderingPlatform.Infrastructure.Data.Configurations
{
    public class OrderStatusHistoryConfiguration : IEntityTypeConfiguration<OrderStatusHistory>
    {
        public void Configure(EntityTypeBuilder<OrderStatusHistory> builder)
        {
            // Relationship to Order configured from the Order side
            // (composition — see OrderConfiguration.HasMany).

            builder.Property(h => h.Status)
                   .HasConversion<string>()
                   .HasMaxLength(30)
                   .IsRequired();

            builder.Property(h => h.ChangedAt).IsRequired();

            builder.HasIndex(h => h.OrderId);

            builder.Property(x => x.Note).HasMaxLength(500);
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
            builder.HasIndex(x => new { x.OrderId, x.ChangedAt });
        }
    }
}
