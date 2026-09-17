namespace RestaurantOrderingPlatform.Domain.Entities;

public class WorkingHour
{
    public Guid Id { get; set; }

    public Guid RestaurantId { get; set; }
    public Restaurant Restaurant { get; set; } = null!;

    public Domain.Enums.DayOfWeek DayOfWeek { get; set; }
    public bool IsClosed { get; set; }

    // Nullable because IsClosed=true days have no open/close time.
    // CloseTime < OpenTime is VALID and means "closes after midnight" —
    // enforced/interpreted in the Service layer's isOpen computation, not here.
    public TimeSpan? OpenTime { get; set; }
    public TimeSpan? CloseTime { get; set; }
}