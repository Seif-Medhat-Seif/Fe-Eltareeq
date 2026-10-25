namespace RestaurantOrderingPlatform.Domain.Enums;

public enum DriverVerificationStatus
{
    NotSubmitted = 0,   // السواق سجّل بس لسه ما بعتش بياناته
    Pending,            // بعت البيانات وبينتظر مراجعة الأدمن
    Approved,           // مسموح يشوف ويقبل توصيلات
    Rejected            // اترفض (ومعاه سبب). يقدر يعدّل ويبعت تاني
}
