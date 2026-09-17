using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RestaurantOrderingPlatform.Domain.Entities;

namespace RestaurantOrderingPlatform.Infrastructure.Data.Configurations;

public class MenuItemConfiguration : IEntityTypeConfiguration<MenuItem>
{
    public void Configure(EntityTypeBuilder<MenuItem> builder)
    {
        builder.ToTable("MenuItems");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(i => i.Description)
            .HasMaxLength(1000);

        builder.Property(i => i.ImageUrl)
            .HasMaxLength(500);

        // Price precision comes from the global ConfigureConventions() rule (18,2) — not repeated here.

        // Global query filter: soft-deleted items never appear in any default query
        // (public menu, owner menu management list, etc.) without an explicit
        // IgnoreQueryFilters() escape hatch — which OrderItem's historical FK lookup
        // is the one legitimate place to use, since it references MenuItem by Id
        // for reporting joins even after the item is soft-deleted.
        builder.HasQueryFilter(i => !i.IsDeleted);

        // NOTE: MenuCategory <-> MenuItem relationship is configured on the
        // MenuCategory side (parent) in MenuCategoryConfiguration — do not
        // duplicate it here (this exact duplication caused the RestaurantId1
        // bug on WorkingHours previously; same class of mistake to avoid).
    }
}
