using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RestaurantOrderingPlatform.Domain.Entities;
using RestaurantOrderingPlatform.Infrastructure.Identity;

namespace RestaurantOrderingPlatform.Infrastructure.Data.Configurations
{
    public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            builder.Property(rt => rt.TokenHash)
                   .IsRequired()
                   .HasMaxLength(64);

            builder.HasIndex(rt => rt.TokenHash).IsUnique();

            // Deliberately NOT unique on UserId — multi-device login.
            builder.HasIndex(rt => rt.UserId);

            builder.HasOne<AppUser>()
                   .WithMany()
                   .HasForeignKey(rt => rt.UserId)
                   .OnDelete(DeleteBehavior.Cascade); // the one deliberate exception — no downstream child records

            builder.Property(rt => rt.ExpiresAt).IsRequired();
            builder.Property(rt => rt.CreatedAt).IsRequired();
            builder.Property(rt => rt.IsRevoked).IsRequired();
        }
    }
}
