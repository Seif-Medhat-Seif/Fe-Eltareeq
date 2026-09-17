using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RestaurantOrderingPlatform.Domain.Entities;

namespace RestaurantOrderingPlatform.Infrastructure.Data.Configurations;

public class MenuCategoryConfiguration : IEntityTypeConfiguration<MenuCategory>
{
    public void Configure(EntityTypeBuilder<MenuCategory> builder)
    {
        builder.ToTable("MenuCategories");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(100);

        // BR: Category names unique per restaurant, not platform-wide.
        builder.HasIndex(c => new { c.RestaurantId, c.Name })
            .IsUnique();

        builder.HasOne(c => c.Restaurant)
            .WithMany(r => r.MenuCategories)
            .HasForeignKey(c => c.RestaurantId)
            .OnDelete(DeleteBehavior.Restrict);

        // Composition: configured from the parent side only (MenuCategory -> Items),
        // never duplicated on MenuItemConfiguration.
        builder.HasMany(c => c.Items)
            .WithOne(i => i.MenuCategory)
            .HasForeignKey(i => i.MenuCategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
