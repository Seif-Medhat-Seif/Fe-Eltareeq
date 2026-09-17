using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RestaurantOrderingPlatform.Domain.Entities;

namespace RestaurantOrderingPlatform.Infrastructure.Data.Configurations
{
    public class WorkingHourConfiguration : IEntityTypeConfiguration<WorkingHour>
    {
        public void Configure(EntityTypeBuilder<WorkingHour> builder)
        {
            // Relationship to Restaurant is configured from the Restaurant side
            // (RestaurantConfiguration.HasMany) since this is a composition —
            // configuring the same FK relationship from both sides would create
            // a conflicting/duplicate mapping.

            // Stored as string so future enum edits never silently shift the
            // meaning of an existing row (same reasoning as OrderStatus).
            builder.Property(w => w.DayOfWeek)
                   .HasConversion<string>()
                   .HasMaxLength(20)
                   .IsRequired();

            builder.Property(w => w.IsClosed).IsRequired();

            // Nullable TimeSpan — meaningless when IsClosed = true.
            // CloseTime < OpenTime is explicitly VALID (overnight hours,
            // e.g. 18:00-02:00) and is interpreted in the Service layer,
            // not rejected here or by FluentValidation.
            builder.Property(w => w.OpenTime);
            builder.Property(w => w.CloseTime);
        }
    }
}
