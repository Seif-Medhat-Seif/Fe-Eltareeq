using RestaurantOrderingPlatform.Application.DTOs.Restaurant;
using RestaurantOrderingPlatform.Application.Exceptions;
using RestaurantOrderingPlatform.Application.Interfaces;
using RestaurantOrderingPlatform.Domain.Enums;
using DomainDayOfWeek = RestaurantOrderingPlatform.Domain.Enums.DayOfWeek;
using RestaurantEntity = RestaurantOrderingPlatform.Domain.Entities.Restaurant;
using WorkingHourEntity = RestaurantOrderingPlatform.Domain.Entities.WorkingHour;

namespace RestaurantOrderingPlatform.Application.Services;

public class RestaurantService : IRestaurantService
{
    private readonly IApplicationDbContext _context;

    public RestaurantService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<RestaurantResponse> CreateAsync(Guid ownerUserId, CreateRestaurantRequest request, CancellationToken ct = default)
    {
        var owner = _context.RestaurantOwners.FirstOrDefault(o => o.UserId == ownerUserId)
            ?? throw new NotFoundException("Restaurant owner account not found.");

        // Pre-check for a clean 409 — the unique index on RestaurantOwnerId
        // is still the real backstop at the DB level.
        if (_context.Restaurants.Any(r => r.RestaurantOwnerId == owner.Id))
            throw new ConflictException("You already have a restaurant. Only one restaurant per owner is allowed.");

        var restaurant = new RestaurantEntity
        {
            Id = Guid.NewGuid(),
            RestaurantOwnerId = owner.Id,
            Name = request.Name,
            Description = request.Description,
            CuisineType = request.CuisineType,
            LogoUrl = request.LogoUrl,
            CoverImageUrl = request.CoverImageUrl,
            Street = request.Street,
            City = request.City,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            DeliveryFee = request.DeliveryFee,
            MinimumOrderAmount = request.MinimumOrderAmount,
            EstimatedDeliveryTimeMinutes = request.EstimatedDeliveryTimeMinutes,
            ManuallyClosed = false, // a brand-new restaurant starts open by default
            HideUnavailableItems = request.HideUnavailableItems,
            CreatedAt = DateTime.UtcNow
        };

        _context.Restaurants.Add(restaurant);
        await _context.SaveChangesAsync(ct);

        return ToResponse(restaurant, workingHours: new List<WorkingHourEntity>());
    }

    public async Task<RestaurantResponse?> GetMineAsync(Guid ownerUserId, CancellationToken ct = default)
    {
        var owner = _context.RestaurantOwners.FirstOrDefault(o => o.UserId == ownerUserId)
            ?? throw new NotFoundException("Restaurant owner account not found.");

        var restaurant = _context.Restaurants.FirstOrDefault(r => r.RestaurantOwnerId == owner.Id);
        if (restaurant is null) return null; // BR-10 — valid state, not an error

        var hours = _context.WorkingHours.Where(w => w.RestaurantId == restaurant.Id).ToList();
        return await Task.FromResult(ToResponse(restaurant, hours));
    }

    public async Task<RestaurantResponse> GetByIdAsync(Guid restaurantId, CancellationToken ct = default)
    {
        var restaurant = _context.Restaurants.FirstOrDefault(r => r.Id == restaurantId)
            ?? throw new NotFoundException("Restaurant not found.");

        var hours = _context.WorkingHours.Where(w => w.RestaurantId == restaurantId).ToList();
        return await Task.FromResult(ToResponse(restaurant, hours));
    }

    public async Task<RestaurantResponse> UpdateAsync(Guid ownerUserId, Guid restaurantId, UpdateRestaurantRequest request, CancellationToken ct = default)
    {
        var restaurant = GetOwnedRestaurantOrThrow(ownerUserId, restaurantId);

        restaurant.Name = request.Name;
        restaurant.Description = request.Description;
        restaurant.CuisineType = request.CuisineType;
        restaurant.LogoUrl = request.LogoUrl;
        restaurant.CoverImageUrl = request.CoverImageUrl;
        restaurant.Street = request.Street;
        restaurant.City = request.City;
        restaurant.Latitude = request.Latitude;
        restaurant.Longitude = request.Longitude;
        restaurant.DeliveryFee = request.DeliveryFee;
        restaurant.MinimumOrderAmount = request.MinimumOrderAmount;
        restaurant.EstimatedDeliveryTimeMinutes = request.EstimatedDeliveryTimeMinutes;
        restaurant.HideUnavailableItems = request.HideUnavailableItems;
        // ManuallyClosed deliberately untouched here — see UpdateRestaurantRequest's comment.

        await _context.SaveChangesAsync(ct);

        var hours = _context.WorkingHours.Where(w => w.RestaurantId == restaurantId).ToList();
        return ToResponse(restaurant, hours);
    }

    public async Task<RestaurantResponse> UpdateStatusAsync(Guid ownerUserId, Guid restaurantId, UpdateRestaurantStatusRequest request, CancellationToken ct = default)
    {
        var restaurant = GetOwnedRestaurantOrThrow(ownerUserId, restaurantId);

        restaurant.ManuallyClosed = request.ManuallyClosed;
        await _context.SaveChangesAsync(ct);

        var hours = _context.WorkingHours.Where(w => w.RestaurantId == restaurantId).ToList();
        return ToResponse(restaurant, hours);
    }

    public async Task<List<WorkingHourDto>> GetWorkingHoursAsync(Guid restaurantId, CancellationToken ct = default)
    {
        if (!_context.Restaurants.Any(r => r.Id == restaurantId))
            throw new NotFoundException("Restaurant not found.");

        var hours = _context.WorkingHours.Where(w => w.RestaurantId == restaurantId).ToList();
        return await Task.FromResult(hours.Select(ToDto).ToList());
    }

    public async Task<List<WorkingHourDto>> UpdateWorkingHoursAsync(Guid ownerUserId, Guid restaurantId, UpdateWorkingHoursRequest request, CancellationToken ct = default)
    {
        var restaurant = GetOwnedRestaurantOrThrow(ownerUserId, restaurantId);

        // Whole-week replace: remove all existing rows, insert the 7 new ones.
        var existing = _context.WorkingHours.Where(w => w.RestaurantId == restaurant.Id).ToList();
        foreach (var row in existing)
            _context.WorkingHours.Remove(row);

        var newRows = request.Days.Select(d => new WorkingHourEntity
        {
            Id = Guid.NewGuid(),
            RestaurantId = restaurant.Id,
            DayOfWeek = d.DayOfWeek,
            IsClosed = d.IsClosed,
            OpenTime = d.IsClosed ? null : d.OpenTime,
            CloseTime = d.IsClosed ? null : d.CloseTime
        }).ToList();

        foreach (var row in newRows)
            _context.WorkingHours.Add(row);

        await _context.SaveChangesAsync(ct);

        return newRows.Select(ToDto).ToList();
    }

    // ---------- private helpers ----------

    private RestaurantEntity GetOwnedRestaurantOrThrow(Guid ownerUserId, Guid restaurantId)
    {
        var owner = _context.RestaurantOwners.FirstOrDefault(o => o.UserId == ownerUserId)
            ?? throw new NotFoundException("Restaurant owner account not found.");

        var restaurant = _context.Restaurants.FirstOrDefault(r => r.Id == restaurantId)
            ?? throw new NotFoundException("Restaurant not found.");

        if (restaurant.RestaurantOwnerId != owner.Id)
            throw new UnauthorizedAppException("You do not own this restaurant.");

        return restaurant;
    }

    private static WorkingHourDto ToDto(WorkingHourEntity w) => new()
    {
        DayOfWeek = w.DayOfWeek,
        IsClosed = w.IsClosed,
        OpenTime = w.OpenTime,
        CloseTime = w.CloseTime
    };

    private static RestaurantResponse ToResponse(RestaurantEntity r, List<WorkingHourEntity> workingHours) => new()
    {
        Id = r.Id,
        Name = r.Name,
        Description = r.Description,
        CuisineType = r.CuisineType,
        LogoUrl = r.LogoUrl,
        CoverImageUrl = r.CoverImageUrl,
        Street = r.Street,
        City = r.City,
        Latitude = r.Latitude,
        Longitude = r.Longitude,
        DeliveryFee = r.DeliveryFee,
        MinimumOrderAmount = r.MinimumOrderAmount,
        EstimatedDeliveryTimeMinutes = r.EstimatedDeliveryTimeMinutes,
        ManuallyClosed = r.ManuallyClosed,
        HideUnavailableItems = r.HideUnavailableItems,
        IsOpen = ComputeIsOpen(r.ManuallyClosed, workingHours),
        CreatedAt = r.CreatedAt
    };

    // The core business rule: manual toggle wins outright when closed;
    // otherwise derived from WorkingHour, with explicit overnight support
    // (CloseTime < OpenTime means "closes after midnight").
    //
    // Deliberately does NOT use DateTime.Now (server-local time) — a
    // server's OS timezone is an environment detail that can silently
    // change (e.g. most Docker/cloud hosts default to UTC), which would
    // shift every restaurant's open/closed status without any code change.
    // Instead: always start from DateTime.UtcNow (a fixed, unambiguous
    // instant) and convert explicitly to Egypt's timezone. Single-country
    // assumption per the Vision doc; a per-restaurant TimeZoneId is the
    // documented future extension point, not implemented here.
    private static readonly TimeZoneInfo EgyptTimeZone = ResolveEgyptTimeZone();

    private static TimeZoneInfo ResolveEgyptTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Africa/Cairo"); // IANA — works cross-platform on .NET 6+/ICU
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time"); // Windows fallback ID
        }
    }

    private static bool ComputeIsOpen(bool manuallyClosed, List<WorkingHourEntity> workingHours)
    {
        if (manuallyClosed) return false;
        if (workingHours.Count == 0) return false; // no hours configured yet — treat as closed, not "always open"

        var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, EgyptTimeZone);
        var currentTime = now.TimeOfDay;
        var today = MapToDomainDayOfWeek(now.DayOfWeek);
        var yesterday = MapToDomainDayOfWeek(now.AddDays(-1).DayOfWeek);

        var todayRow = workingHours.FirstOrDefault(w => w.DayOfWeek == today);
        var yesterdayRow = workingHours.FirstOrDefault(w => w.DayOfWeek == yesterday);

        if (todayRow is { IsClosed: false, OpenTime: not null, CloseTime: not null })
        {
            if (todayRow.CloseTime > todayRow.OpenTime)
            {
                // normal same-day window, e.g. 09:00-22:00
                if (currentTime >= todayRow.OpenTime && currentTime < todayRow.CloseTime) return true;
            }
            else
            {
                // overnight window starting today, e.g. 18:00-02:00 —
                // "open" for the rest of today from OpenTime onward
                if (currentTime >= todayRow.OpenTime) return true;
            }
        }

        // Spillover from yesterday's overnight window into today, before CloseTime
        if (yesterdayRow is { IsClosed: false, OpenTime: not null, CloseTime: not null }
            && yesterdayRow.CloseTime < yesterdayRow.OpenTime
            && currentTime < yesterdayRow.CloseTime)
        {
            return true;
        }

        return false;
    }

    private static DomainDayOfWeek MapToDomainDayOfWeek(System.DayOfWeek day) => day switch
    {
        System.DayOfWeek.Monday => DomainDayOfWeek.Monday,
        System.DayOfWeek.Tuesday => DomainDayOfWeek.Tuesday,
        System.DayOfWeek.Wednesday => DomainDayOfWeek.Wednesday,
        System.DayOfWeek.Thursday => DomainDayOfWeek.Thursday,
        System.DayOfWeek.Friday => DomainDayOfWeek.Friday,
        System.DayOfWeek.Saturday => DomainDayOfWeek.Saturday,
        System.DayOfWeek.Sunday => DomainDayOfWeek.Sunday,
        _ => throw new ArgumentOutOfRangeException(nameof(day))
    };
}
