using RestaurantOrderingPlatform.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestaurantOrderingPlatform.Application.Services;

public static class RestaurantAvailability
{
    private static readonly TimeZoneInfo Cairo = ResolveCairo();

    private static TimeZoneInfo ResolveCairo()
    {
        // ★ الـ ID بيختلف بين Windows و Linux. الكود اللي بيستخدم
        //   "Africa/Cairo" على طول بيقع على Windows من غير ICU.
        foreach (var id in new[] { "Africa/Cairo", "Egypt Standard Time" })
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
            catch (TimeZoneNotFoundException) { }
        }
        return TimeZoneInfo.Utc;
    }

    public static bool IsOpen(Restaurant restaurant)
    {
        if (restaurant.ManuallyClosed) return false;

        var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Cairo);
        var today = ToDomainDay(now.DayOfWeek);
        var time = now.TimeOfDay;

        // اليوم الحالي
        var wh = restaurant.WorkingHours.FirstOrDefault(w => w.DayOfWeek == today);
        if (wh is { IsClosed: false, OpenTime: { } open, CloseTime: { } close })
        {
            if (close > open && time >= open && time < close) return true;
            if (close <= open && time >= open) return true;      // ليلة ممتدة
        }

        // امبارح — لو كان بيقفل بعد منتصف الليل
        var yesterday = ToDomainDay(now.AddDays(-1).DayOfWeek);
        var ywh = restaurant.WorkingHours.FirstOrDefault(w => w.DayOfWeek == yesterday);
        if (ywh is { IsClosed: false, OpenTime: { } yopen, CloseTime: { } yclose }
            && yclose <= yopen && time < yclose) return true;

        return false;
    }

    private static Domain.Enums.DayOfWeek ToDomainDay(System.DayOfWeek d) => d switch
    {
        System.DayOfWeek.Monday => Domain.Enums.DayOfWeek.Monday,
        System.DayOfWeek.Tuesday => Domain.Enums.DayOfWeek.Tuesday,
        System.DayOfWeek.Wednesday => Domain.Enums.DayOfWeek.Wednesday,
        System.DayOfWeek.Thursday => Domain.Enums.DayOfWeek.Thursday,
        System.DayOfWeek.Friday => Domain.Enums.DayOfWeek.Friday,
        System.DayOfWeek.Saturday => Domain.Enums.DayOfWeek.Saturday,
        _ => Domain.Enums.DayOfWeek.Sunday
    };
}
