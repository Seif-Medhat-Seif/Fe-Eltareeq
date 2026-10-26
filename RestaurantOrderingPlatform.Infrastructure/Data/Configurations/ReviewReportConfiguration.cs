using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RestaurantOrderingPlatform.Domain.Entities;

namespace RestaurantOrderingPlatform.Infrastructure.Data.Configurations
{
    public class ReviewReportConfiguration : IEntityTypeConfiguration<ReviewReport>
    {
        public void Configure(EntityTypeBuilder<ReviewReport> builder)
        {
            // Relationship to Review configured from the Review side
            // (composition — see ReviewConfiguration.HasMany).

            builder.HasOne(rr => rr.RestaurantOwner)
                   .WithMany()   // RestaurantOwner doesn't expose a Reports collection — not a useful traversal
                   .HasForeignKey(rr => rr.RestaurantOwnerId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.Property(rr => rr.Reason).IsRequired().HasMaxLength(500);

            builder.Property(rr => rr.Status)
                   .HasConversion<string>()
                   .HasMaxLength(20)
                   .IsRequired();

            builder.HasIndex(x => new { x.ReviewId, x.RestaurantOwnerId })
                   .HasFilter("[Status] = 'Open'").IsUnique();   // بلاغ مفتوح واحد لكل owner لكل مراجعة

            builder.HasIndex(x => x.ReviewId);   // الـ filtered index بيغطي Open بس

            builder.Property(rr => rr.CreatedAt).IsRequired();
        }
    }
}
