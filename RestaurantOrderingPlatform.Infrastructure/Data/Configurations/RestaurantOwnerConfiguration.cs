using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RestaurantOrderingPlatform.Domain.Entities;
using RestaurantOrderingPlatform.Infrastructure.Identity;

namespace RestaurantOrderingPlatform.Infrastructure.Data.Configurations
{
    public class RestaurantOwnerConfiguration : IEntityTypeConfiguration<RestaurantOwner>
    {
        public void Configure(EntityTypeBuilder<RestaurantOwner> builder)
        {
            builder.HasIndex(o => o.UserId).IsUnique();

            builder.HasOne<AppUser>()
                   .WithMany()
                   .HasForeignKey(o => o.UserId)
                   .OnDelete(DeleteBehavior.Restrict);

            // The Restaurant <-> RestaurantOwner 1:1 relationship is fully
            // configured from the Restaurant side (RestaurantConfiguration),
            // since Restaurant holds the FK (RestaurantOwnerId).
        }
    }
}
