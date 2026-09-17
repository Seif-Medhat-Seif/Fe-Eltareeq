using RestaurantOrderingPlatform.Domain.Enums;

namespace RestaurantOrderingPlatform.Application.DTOs.Restaurant;

public class WorkingHourDto
{
    public Domain.Enums.DayOfWeek DayOfWeek { get; set; }
    public bool IsClosed { get; set; }

    // Nullable — meaningless when IsClosed = true.
    // CloseTime < OpenTime is EXPLICITLY VALID here (overnight hours,
    // e.g. 18:00-02:00) — the Validator does not reject it.
    public TimeSpan? OpenTime { get; set; }
    public TimeSpan? CloseTime { get; set; }
}

public class UpdateWorkingHoursRequest
{
    // Whole-week replace only (locked Phase 4 behavior) — always exactly 7 entries, one per day.
    public List<WorkingHourDto> Days { get; set; } = new();
}
