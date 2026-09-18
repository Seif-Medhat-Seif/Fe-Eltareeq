// Infrastructure/Data/Configurations/ProcessedWebhookConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RestaurantOrderingPlatform.Domain.Entities;

namespace RestaurantOrderingPlatform.Infrastructure.Data.Configurations;

public class ProcessedWebhookConfiguration : IEntityTypeConfiguration<ProcessedWebhook>
{
    public void Configure(EntityTypeBuilder<ProcessedWebhook> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Provider)
               .IsRequired()
               .HasMaxLength(50);

        builder.Property(x => x.ExternalEventId)
               .IsRequired()
               .HasMaxLength(100);

        builder.Property(x => x.Outcome)
               .HasMaxLength(200);

        builder.Property(x => x.ReceivedAt).IsRequired();

        // ★★★ الضمان الحقيقي ضد الـ duplicate webhook ★★★
        // مش if-check في الكود (ده race condition جاهز)، ده UNIQUE constraint
        // على مستوى الداتابيز. لو اتنين webhook وصلوا في نفس الميلي ثانية،
        // واحد بس هينجح في الـ INSERT والتاني هيرمي DbUpdateException.
        builder.HasIndex(x => new { x.Provider, x.ExternalEventId })
               .IsUnique()
               .HasDatabaseName("UX_ProcessedWebhooks_Provider_ExternalEventId");

        builder.HasIndex(x => x.OrderId);
    }
}
