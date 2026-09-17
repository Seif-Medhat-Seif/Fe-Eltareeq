namespace RestaurantOrderingPlatform.Application.DTOs.Menu;

// ---------- Category ----------

public record CreateMenuCategoryRequest(string Name, int DisplayOrder);

public record UpdateMenuCategoryRequest(string Name, int DisplayOrder);

public record MenuCategoryResponse(
    Guid Id,
    Guid RestaurantId,
    string Name,
    int DisplayOrder);

// ---------- Menu Item ----------

public record CreateMenuItemRequest(
    string Name,
    string? Description,
    decimal Price,
    string? ImageUrl);

public record UpdateMenuItemRequest(
    string Name,
    string? Description,
    decimal Price,
    string? ImageUrl);

public record SetMenuItemAvailabilityRequest(bool IsAvailable);

public record MenuItemResponse(
    Guid Id,
    Guid MenuCategoryId,
    string Name,
    string? Description,
    decimal Price,
    string? ImageUrl,
    bool IsAvailable);

// ---------- Composite / public menu ----------

/// <summary>
/// Nested category + items shape returned by GET /api/restaurants/{id}/categories/{catId}/... style calls
/// and reused inside RestaurantMenuResponse.
/// </summary>
public record MenuCategoryWithItemsResponse(
    Guid Id,
    string Name,
    int DisplayOrder,
    IReadOnlyList<MenuItemResponse> Items);

/// <summary>
/// GET /api/restaurants/{id}/menu — full menu in one call: categories WITH nested items.
/// Respects Restaurant.HideUnavailableItems: when true, unavailable items are excluded
/// entirely instead of returned with IsAvailable=false for the client to gray out.
/// </summary>
public record RestaurantMenuResponse(
    Guid RestaurantId,
    bool HideUnavailableItems,
    IReadOnlyList<MenuCategoryWithItemsResponse> Categories);
