using RestaurantOrderingPlatform.Domain.Enums;

namespace RestaurantOrderingPlatform.Domain.Entities;

public class Review
{
    public Guid Id { get; set; }

    // Unique — one review per order (Phase 4 constraint).
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;

    public Guid RestaurantId { get; set; }
    public Restaurant Restaurant { get; set; } = null!;

    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public int Rating { get; set; }
    public string? Comment { get; set; }

    public string? OwnerReply { get; set; }

    public ReviewModerationStatus ModerationStatus { get; set; } = ReviewModerationStatus.Visible;

    public DateTime CreatedAt { get; set; }

    public ICollection<ReviewReport> Reports { get; set; } = new List<ReviewReport>();
}
