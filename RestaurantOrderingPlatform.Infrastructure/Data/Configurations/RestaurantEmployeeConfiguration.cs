using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RestaurantOrderingPlatform.Domain.Entities;

namespace RestaurantOrderingPlatform.Infrastructure.Data.Configurations
{
    public class RestaurantEmployeeConfiguration : IEntityTypeConfiguration<RestaurantEmployee>
    {
        public void Configure(EntityTypeBuilder<RestaurantEmployee> builder)
        {
            // Relationships to Restaurant and Customer are configured from
            // their respective sides (RestaurantConfiguration.HasMany,
            // CustomerConfiguration.HasMany).

            builder.Property(e => e.IsActive).IsRequired();
            builder.Property(e => e.AssignedAt).IsRequired();

            // THE single most business-critical index in the schema (BR-2):
            // filtered unique index — many inactive historical rows per
            // customer, but only ONE active one, ever.
            builder.HasIndex(e => e.CustomerId)
                   .IsUnique()
                   .HasFilter("[IsActive] = 1");
        }
    }
}
