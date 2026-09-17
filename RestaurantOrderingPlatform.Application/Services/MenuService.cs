using Microsoft.EntityFrameworkCore;
using RestaurantOrderingPlatform.Application.DTOs.Menu;
using RestaurantOrderingPlatform.Application.Exceptions;
using RestaurantOrderingPlatform.Application.Interfaces;
using RestaurantOrderingPlatform.Domain.Entities;

namespace RestaurantOrderingPlatform.Application.Services;

public class MenuService : IMenuService
{
    private readonly IApplicationDbContext _context;

    public MenuService(IApplicationDbContext context)
    {
        _context = context;
    }

    // ---------------- Categories ----------------

    public async Task<MenuCategoryResponse> CreateCategoryAsync(Guid ownerUserId, Guid restaurantId, CreateMenuCategoryRequest request)
    {
        var restaurant = await GetOwnedRestaurantAsync(ownerUserId, restaurantId);

        var nameTaken = await _context.MenuCategories
            .AnyAsync(c => c.RestaurantId == restaurant.Id
                        && c.Name.ToUpper() == request.Name.ToUpper());
        if (nameTaken)
            throw new ConflictException($"A category named '{request.Name}' already exists for this restaurant.");

        var category = new MenuCategory
        {
            Id = Guid.NewGuid(),
            RestaurantId = restaurant.Id,
            Name = request.Name,
            DisplayOrder = request.DisplayOrder
        };

        _context.MenuCategories.Add(category);
        await _context.SaveChangesAsync();

        return ToResponse(category);
    }

    public async Task<MenuCategoryResponse> UpdateCategoryAsync(Guid ownerUserId, Guid categoryId, UpdateMenuCategoryRequest request)
    {
        var category = await _context.MenuCategories
            .Include(c => c.Restaurant)
            .FirstOrDefaultAsync(c => c.Id == categoryId)
            ?? throw new NotFoundException("Menu category not found.");

        await EnsureOwnsRestaurantAsync(ownerUserId, category.RestaurantId);

        var nameTaken = await _context.MenuCategories
            .AnyAsync(c => c.RestaurantId == category.RestaurantId
                        && c.Id != category.Id
                        && c.Name.ToUpper() == request.Name.ToUpper());
        if (nameTaken)
            throw new ConflictException($"A category named '{request.Name}' already exists for this restaurant.");

        category.Name = request.Name;
        category.DisplayOrder = request.DisplayOrder;

        await _context.SaveChangesAsync();

        return ToResponse(category);
    }

    public async Task DeleteCategoryAsync(Guid ownerUserId, Guid categoryId)
    {
        var category = await _context.MenuCategories
            .FirstOrDefaultAsync(c => c.Id == categoryId)
            ?? throw new NotFoundException("Menu category not found.");

        await EnsureOwnsRestaurantAsync(ownerUserId, category.RestaurantId);

        // NOTE: MenuItems is filtered by the global soft-delete query filter, so a
        // category whose only items are soft-deleted is correctly treated as empty
        // and can be deleted — deleted items keep their historical FK to the
        // category id regardless (categories themselves are never soft-deleted
        // in V1; hard delete is acceptable once genuinely empty).
        var hasItems = await _context.MenuItems.AnyAsync(i => i.MenuCategoryId == categoryId);
        if (hasItems)
            throw new ConflictException("Cannot delete a category that still has menu items. Remove or reassign its items first.");

        _context.MenuCategories.Remove(category);
        await _context.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<MenuCategoryResponse>> GetCategoriesAsync(Guid restaurantId)
    {
        return await _context.MenuCategories
            .Where(c => c.RestaurantId == restaurantId)
            .OrderBy(c => c.DisplayOrder)
            .Select(c => new MenuCategoryResponse(c.Id, c.RestaurantId, c.Name, c.DisplayOrder))
            .ToListAsync();
    }

    // ---------------- Items ----------------

    public async Task<MenuItemResponse> CreateItemAsync(Guid ownerUserId, Guid categoryId, CreateMenuItemRequest request)
    {
        var category = await _context.MenuCategories
            .FirstOrDefaultAsync(c => c.Id == categoryId)
            ?? throw new NotFoundException("Menu category not found.");

        await EnsureOwnsRestaurantAsync(ownerUserId, category.RestaurantId);

        var item = new MenuItem
        {
            Id = Guid.NewGuid(),
            MenuCategoryId = category.Id,
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            ImageUrl = request.ImageUrl,
            IsAvailable = true,
            IsDeleted = false
        };

        _context.MenuItems.Add(item);
        await _context.SaveChangesAsync();

        return ToResponse(item);
    }

    public async Task<MenuItemResponse> UpdateItemAsync(Guid ownerUserId, Guid itemId, UpdateMenuItemRequest request)
    {
        var item = await GetOwnedItemAsync(ownerUserId, itemId);

        // Snapshot principle: this NEVER touches existing OrderItem.ItemNameSnapshot /
        // ItemPriceSnapshot rows — those were copied at order time and are immutable.
        item.Name = request.Name;
        item.Description = request.Description;
        item.Price = request.Price;
        item.ImageUrl = request.ImageUrl;

        await _context.SaveChangesAsync();

        return ToResponse(item);
    }

    public async Task<MenuItemResponse> SetItemAvailabilityAsync(Guid ownerUserId, Guid itemId, SetMenuItemAvailabilityRequest request)
    {
        var item = await GetOwnedItemAsync(ownerUserId, itemId);

        item.IsAvailable = request.IsAvailable;
        await _context.SaveChangesAsync();

        return ToResponse(item);
    }

    public async Task DeleteItemAsync(Guid ownerUserId, Guid itemId)
    {
        var item = await GetOwnedItemAsync(ownerUserId, itemId);

        // Soft delete only — preserves OrderItem.MenuItemId FK integrity for
        // historical orders/reports (ItemNameSnapshot/ItemPriceSnapshot on OrderItem
        // are what actually gets displayed, so this is safe).
        item.IsDeleted = true;
        item.IsAvailable = false;

        await _context.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<MenuItemResponse>> GetItemsByCategoryAsync(Guid categoryId)
    {
        // Global query filter on MenuItem already excludes IsDeleted rows.
        return await _context.MenuItems
            .Where(i => i.MenuCategoryId == categoryId)
            .Select(i => new MenuItemResponse(i.Id, i.MenuCategoryId, i.Name, i.Description, i.Price, i.ImageUrl, i.IsAvailable))
            .ToListAsync();
    }

    // ---------------- Public composite ----------------

    public async Task<RestaurantMenuResponse> GetFullMenuAsync(Guid restaurantId)
    {
        var restaurant = await _context.Restaurants
            .FirstOrDefaultAsync(r => r.Id == restaurantId)
            ?? throw new NotFoundException("Restaurant not found.");

        var categories = await _context.MenuCategories
            .Where(c => c.RestaurantId == restaurantId)
            .OrderBy(c => c.DisplayOrder)
            .Include(c => c.Items)
            .ToListAsync();

        var result = categories.Select(c =>
        {
            var items = c.Items.AsEnumerable();

            // HideUnavailableItems=true -> exclude unavailable items entirely.
            // HideUnavailableItems=false -> return them with IsAvailable=false so
            // the client can gray them out (industry-standard convention).
            if (restaurant.HideUnavailableItems)
                items = items.Where(i => i.IsAvailable);

            var itemResponses = items
                .Select(i => new MenuItemResponse(i.Id, i.MenuCategoryId, i.Name, i.Description, i.Price, i.ImageUrl, i.IsAvailable))
                .ToList();

            return new MenuCategoryWithItemsResponse(c.Id, c.Name, c.DisplayOrder, itemResponses);
        }).ToList();

        return new RestaurantMenuResponse(restaurant.Id, restaurant.HideUnavailableItems, result);
    }

    // ---------------- Helpers ----------------

    private async Task<Restaurant> GetOwnedRestaurantAsync(Guid ownerUserId, Guid restaurantId)
    {
        var restaurant = await _context.Restaurants
            .Include(r => r.RestaurantOwner)
            .FirstOrDefaultAsync(r => r.Id == restaurantId)
            ?? throw new NotFoundException("Restaurant not found.");

        if (restaurant.RestaurantOwner.UserId != ownerUserId)
            throw new UnauthorizedAppException("You do not have permission to manage this restaurant.");

        return restaurant;
    }

    private async Task EnsureOwnsRestaurantAsync(Guid ownerUserId, Guid restaurantId)
    {
        var owns = await _context.Restaurants
            .Include(r => r.RestaurantOwner)
            .AnyAsync(r => r.Id == restaurantId && r.RestaurantOwner.UserId == ownerUserId);

        if (!owns)
            throw new UnauthorizedAppException("You do not have permission to manage this restaurant.");
    }

    private async Task<MenuItem> GetOwnedItemAsync(Guid ownerUserId, Guid itemId)
    {
        var item = await _context.MenuItems
            .Include(i => i.MenuCategory)
            .FirstOrDefaultAsync(i => i.Id == itemId)
            ?? throw new NotFoundException("Menu item not found.");

        await EnsureOwnsRestaurantAsync(ownerUserId, item.MenuCategory.RestaurantId);

        return item;
    }

    private static MenuCategoryResponse ToResponse(MenuCategory c) =>
        new(c.Id, c.RestaurantId, c.Name, c.DisplayOrder);

    private static MenuItemResponse ToResponse(MenuItem i) =>
        new(i.Id, i.MenuCategoryId, i.Name, i.Description, i.Price, i.ImageUrl, i.IsAvailable);
}
