using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RestaurantOrderingPlatform.Domain.Entities;

namespace RestaurantOrderingPlatform.Infrastructure.Data.Configurations
{
    public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
    {
        public void Configure(EntityTypeBuilder<Payment> builder)
        {
            // Relationship to Order configured from the Order side
            // (composition — see OrderConfiguration.HasMany).

            builder.HasIndex(p => p.OrderId); // deliberately NOT unique — supports retries

            builder.Property(p => p.Status)
                   .HasConversion<string>()
                   .HasMaxLength(30)
                   .IsRequired();

            builder.Property(p => p.AmountPaid).IsRequired();
            builder.Property(p => p.PaymobTransactionReference).HasMaxLength(100);
            builder.Property(p => p.CreatedAt).IsRequired();
        }
    }
}
