using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RestaurantOrderingPlatform.Infrastructure.Identity;

namespace RestaurantOrderingPlatform.Infrastructure.Data.Configurations
{
    public class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
    {
        public void Configure(EntityTypeBuilder<AppUser> builder)
        {
            builder.Property(u => u.FullName)
                   .IsRequired()
                   .HasMaxLength(100);

            builder.Property(u => u.CreatedAt)
                   .IsRequired();
        }
    }
}
