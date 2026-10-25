using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RestaurantOrderingPlatform.Domain.Entities;

namespace RestaurantOrderingPlatform.Infrastructure.Data.Configurations
{
    public class ReviewConfiguration : IEntityTypeConfiguration<Review>
    {
        public void Configure(EntityTypeBuilder<Review> builder)
        {
            // The Review <-> Order 1:1 relationship (including the unique
            // index on OrderId) is fully configured from the Order side
            // (OrderConfiguration.HasOne(o => o.Review)).

            // Relationship to Restaurant configured from the Restaurant side
            // (RestaurantConfiguration.HasMany).
            // Relationship to Customer configured from the Customer side
            // (CustomerConfiguration.HasMany).

            builder.Property(r => r.Rating).IsRequired();
            builder.Property(r => r.Comment).HasMaxLength(1000);
            builder.Property(r => r.OwnerReply).HasMaxLength(1000);

            builder.Property(r => r.ModerationStatus)
                   .HasConversion<string>()
                   .HasMaxLength(20)
                   .IsRequired();

            builder.Property(r => r.CreatedAt).IsRequired();

            builder.HasIndex(r => r.OrderId).IsUnique();                       // موجود
            builder.HasIndex(r => new { r.RestaurantId, r.ModerationStatus, r.CreatedAt }); // للـ list العام
            builder.ToTable(t => t.HasCheckConstraint("CK_Review_Rating", "[Rating] BETWEEN 1 AND 5"));
            // Composition: a ReviewReport has no meaning without its Review.
            builder.HasMany(r => r.Reports)
                   .WithOne(rr => rr.Review)
                   .HasForeignKey(rr => rr.ReviewId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
