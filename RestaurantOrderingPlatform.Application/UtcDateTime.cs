namespace RestaurantOrderingPlatform.Application;

public static class UtcDateTime
{
    /// <summary>
    /// بيحوّل أي DateTime لـ UTC من غير تخمين. القيمة اللي Kind بتاعها Unspecified (عميل بعت تاريخ من غير Z)
    /// بنعتبرها UTC بدل ما ToUniversalTime() يفترض إنها توقيت السيرفر (وده بيختلف بين جهازك والـ production).
    /// </summary>
    public static DateTime Normalize(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };
}
