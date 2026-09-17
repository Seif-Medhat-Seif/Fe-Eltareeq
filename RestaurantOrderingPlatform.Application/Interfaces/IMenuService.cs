using RestaurantOrderingPlatform.Application.DTOs.Menu;

namespace RestaurantOrderingPlatform.Application.Interfaces;

/// <summary>
/// Covers both MenuCategory and MenuItem — one Service per Phase 5 module boundary,
/// not per entity (matches Phase 6 naming/service-strategy decision).
/// Every Owner-scoped method takes the acting owner's UserId and internally re-derives
/// their Restaurant + verifies ownership — it never trusts a restaurantId alone.
/// </summary>
public interface IMenuService
{
    // Categories
    Task<MenuCategoryResponse> CreateCategoryAsync(Guid ownerUserId, Guid restaurantId, CreateMenuCategoryRequest request);
    Task<MenuCategoryResponse> UpdateCategoryAsync(Guid ownerUserId, Guid categoryId, UpdateMenuCategoryRequest request);
    Task DeleteCategoryAsync(Guid ownerUserId, Guid categoryId);
    Task<IReadOnlyList<MenuCategoryResponse>> GetCategoriesAsync(Guid restaurantId);

    // Items
    Task<MenuItemResponse> CreateItemAsync(Guid ownerUserId, Guid categoryId, CreateMenuItemRequest request);
    Task<MenuItemResponse> UpdateItemAsync(Guid ownerUserId, Guid itemId, UpdateMenuItemRequest request);
    Task<MenuItemResponse> SetItemAvailabilityAsync(Guid ownerUserId, Guid itemId, SetMenuItemAvailabilityRequest request);
    Task DeleteItemAsync(Guid ownerUserId, Guid itemId);
    Task<IReadOnlyList<MenuItemResponse>> GetItemsByCategoryAsync(Guid categoryId);

    // Public composite
    Task<RestaurantMenuResponse> GetFullMenuAsync(Guid restaurantId);
}
