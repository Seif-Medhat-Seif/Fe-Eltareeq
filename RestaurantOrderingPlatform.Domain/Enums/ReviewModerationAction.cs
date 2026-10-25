namespace RestaurantOrderingPlatform.Domain.Enums;

/// <summary>إجراء الأدمن على تقرير مراجعة. مش بيتخزن في الداتابيز، بيدخل في الـ request بس.</summary>
public enum ReviewModerationAction
{
    Hide,      // soft: ModerationStatus = Hidden
    Delete,    // hard delete
    Dismiss    // تجاهل البلاغ من غير أي إجراء على المراجعة
}
