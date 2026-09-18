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

            builder.Property(x => x.PaymobOrderId).HasMaxLength(100);
            builder.Property(x => x.FailureReason).HasMaxLength(500);
            builder.Property(x => x.PaymobTransactionReference).HasMaxLength(100);

            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);

            // transaction reference لازم يبقى فريد لو موجود
            builder.HasIndex(x => x.PaymobTransactionReference)
                   .IsUnique()
                   .HasFilter("[PaymobTransactionReference] IS NOT NULL");

            builder.HasIndex(x => x.OrderId);
        }
    }
}
