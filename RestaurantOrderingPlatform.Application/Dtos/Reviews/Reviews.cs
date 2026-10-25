using RestaurantOrderingPlatform.Domain.Enums;

namespace RestaurantOrderingPlatform.Application.DTOs.Reviews;

public record CreateReviewRequest(int Rating, string? Comment);

public record ModerateReviewRequest(ReviewModerationAction Action);

public record ReplyToReviewRequest(string Reply);

/// <summary>شكل الأدمن: المراجعة + البلاغات المفتوحة عليها.</summary>
public record ReportedReviewResponse(
    Guid Id,
    Guid RestaurantId,
    string RestaurantName,
    int Rating,
    string? Comment,
    string? OwnerReply,
    string ReviewerName,
    ReviewModerationStatus ModerationStatus,
    DateTime CreatedAt,
    IReadOnlyList<ReviewReportResponse> OpenReports);

public record ReportReviewRequest(string Reason);

public record ReviewReportResponse(Guid Id, string Reason, DateTime CreatedAt);
/// <summary>الشكل العام (Public). الاسم الأول بس للعميل لحماية الخصوصية.</summary>
public record ReviewResponse(
    Guid Id,
    Guid RestaurantId,
    int Rating,
    string? Comment,
    string? OwnerReply,
    string ReviewerName,
    DateTime CreatedAt);