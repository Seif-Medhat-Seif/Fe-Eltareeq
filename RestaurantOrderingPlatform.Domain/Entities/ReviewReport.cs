using RestaurantOrderingPlatform.Domain.Enums;

namespace RestaurantOrderingPlatform.Domain.Entities;

public class ReviewReport
{
    public Guid Id { get; set; }

    public Guid ReviewId { get; set; }
    public Review Review { get; set; } = null!;

    // The owner who filed the report — not a Customer, an owner reporting
    // abuse on their own restaurant's review.
    public Guid RestaurantOwnerId { get; set; }
    public RestaurantOwner RestaurantOwner { get; set; } = null!;

    public string Reason { get; set; } = string.Empty;
    public ReviewReportStatus Status { get; set; } = ReviewReportStatus.Open;

    public DateTime CreatedAt { get; set; }
}
