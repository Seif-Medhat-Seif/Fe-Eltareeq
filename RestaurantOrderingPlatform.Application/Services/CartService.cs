using Microsoft.EntityFrameworkCore;
using RestaurantOrderingPlatform.Application.DTOs.Cart;
using RestaurantOrderingPlatform.Application.Exceptions;
using RestaurantOrderingPlatform.Application.Interfaces;
using RestaurantOrderingPlatform.Domain.Entities;
using RestaurantOrderingPlatform.Domain.Enums;

namespace RestaurantOrderingPlatform.Application.Services;

public class CartService : ICartService
{
    private readonly IApplicationDbContext _context;

    public CartService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CartResponse> GetMyCartAsync(Guid userId)
    {
        var cart = await GetOrCreateCartAsync(userId);
        return await BuildResponseAsync(cart.Id);
    }

    public async Task<CartResponse> AddItemAsync(Guid userId, AddCartItemRequest request)
    {
        var cart = await GetOrCreateCartAsync(userId);

        var menuItem = await _context.MenuItems
            .Include(m => m.MenuCategory)
            .FirstOrDefaultAsync(m => m.Id == request.MenuItemId)
            ?? throw new NotFoundException("Menu item not found.");

        if (!menuItem.IsAvailable)
            throw new ConflictException("This item is currently unavailable.");

        var itemRestaurantId = menuItem.MenuCategory.RestaurantId;

        // The core conflict rule: cart is bound to a single restaurant.
        if (cart.RestaurantId.HasValue && cart.RestaurantId.Value != itemRestaurantId)
        {
            if (!request.ConfirmReplace)
            {
                var currentRestaurant = await _context.Restaurants.FirstAsync(r => r.Id == cart.RestaurantId.Value);
                throw new ConflictException(
                    $"Your cart contains items from '{currentRestaurant.Name}'. " +
                    "Resend with confirmReplace=true to replace it with items from this restaurant.");
            }

            // Confirmed replace: wipe the old cart contents and coupon (BR-7: coupon
            // is restaurant-specific, cannot survive a restaurant switch).
            var oldItems = _context.CartItems.Where(ci => ci.CartId == cart.Id);
            _context.CartItems.RemoveRange(oldItems);
            cart.CouponId = null;
        }

        cart.RestaurantId = itemRestaurantId;

        var existing = await _context.CartItems
            .FirstOrDefaultAsync(ci => ci.CartId == cart.Id && ci.MenuItemId == menuItem.Id);

        if (existing is not null)
        {
            existing.Quantity += request.Quantity;
            existing.PriceAtAdd = menuItem.Price; // re-baseline on any quantity change
        }
        else
        {
            _context.CartItems.Add(new CartItem
            {
                Id = Guid.NewGuid(),
                CartId = cart.Id,
                MenuItemId = menuItem.Id,
                Quantity = request.Quantity,
                PriceAtAdd = menuItem.Price
            });
        }

        await _context.SaveChangesAsync();
        return await BuildResponseAsync(cart.Id);
    }

    public async Task<CartResponse> UpdateItemAsync(Guid userId, Guid cartItemId, UpdateCartItemRequest request)
    {
        var (cart, item) = await GetOwnedCartItemAsync(userId, cartItemId);

        item.Quantity = request.Quantity;
        await _context.SaveChangesAsync();

        return await BuildResponseAsync(cart.Id);
    }

    public async Task<CartResponse> RemoveItemAsync(Guid userId, Guid cartItemId)
    {
        var (cart, item) = await GetOwnedCartItemAsync(userId, cartItemId);

        _context.CartItems.Remove(item);

        // If the cart is now empty, clear RestaurantId/CouponId so it's a genuinely
        // empty cart again, not a "phantom" restaurant lock with zero items.
        var remaining = await _context.CartItems.CountAsync(ci => ci.CartId == cart.Id && ci.Id != item.Id);
        if (remaining == 0)
        {
            cart.RestaurantId = null;
            cart.CouponId = null;
        }

        await _context.SaveChangesAsync();
        return await BuildResponseAsync(cart.Id);
    }

    public async Task<CartResponse> ClearCartAsync(Guid userId)
    {
        var cart = await GetOrCreateCartAsync(userId);

        var items = _context.CartItems.Where(ci => ci.CartId == cart.Id);
        _context.CartItems.RemoveRange(items);
        cart.RestaurantId = null;
        cart.CouponId = null;

        await _context.SaveChangesAsync();
        return await BuildResponseAsync(cart.Id);
    }

    public async Task<CartResponse> ApplyCouponAsync(Guid userId, ApplyCouponRequest request)
    {
        var cart = await GetOrCreateCartAsync(userId);

        if (cart.RestaurantId is null)
            throw new ConflictException("Add items to your cart before applying a coupon.");

        var normalizedCode = request.Code.Trim().ToUpperInvariant();

        var coupon = await _context.Coupons.FirstOrDefaultAsync(c =>
                c.RestaurantId == cart.RestaurantId.Value && c.Code == normalizedCode)
            ?? throw new NotFoundException("Coupon not found for this restaurant.");

        if (!coupon.IsActive)
            throw new ConflictException("This coupon is no longer active.");

        if (coupon.ExpiryDate.HasValue && coupon.ExpiryDate.Value < DateTime.UtcNow)
            throw new ConflictException("This coupon has expired.");

        var subtotal = await GetSubtotalAsync(cart.Id);

        if (coupon.MinimumOrderAmount.HasValue && subtotal < coupon.MinimumOrderAmount.Value)
            throw new ConflictException($"This coupon requires a minimum order of {coupon.MinimumOrderAmount.Value:0.00}.");

        // NOTE: UsageLimitTotal / UsageLimitPerCustomer are NOT checked here — they
        // require CouponUsage/Order data that doesn't exist until Module 6/8. Final,
        // authoritative validation happens atomically at checkout. See Module 5
        // handoff note — this is a deliberate, disclosed gap, not an oversight.

        cart.CouponId = coupon.Id;
        await _context.SaveChangesAsync();

        return await BuildResponseAsync(cart.Id);
    }

    public async Task<CartResponse> RemoveCouponAsync(Guid userId)
    {
        var cart = await GetOrCreateCartAsync(userId);
        cart.CouponId = null;
        await _context.SaveChangesAsync();

        return await BuildResponseAsync(cart.Id);
    }

    // ---------------- Helpers ----------------

    private async Task<Cart> GetOrCreateCartAsync(Guid userId)
    {
        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == userId)
            ?? throw new NotFoundException("Customer profile not found for this account.");

        var cart = await _context.Carts.FirstOrDefaultAsync(c => c.CustomerId == customer.Id);
        if (cart is not null)
            return cart;

        cart = new Cart { Id = Guid.NewGuid(), CustomerId = customer.Id };
        _context.Carts.Add(cart);
        await _context.SaveChangesAsync();
        return cart;
    }

    private async Task<(Cart Cart, CartItem Item)> GetOwnedCartItemAsync(Guid userId, Guid cartItemId)
    {
        var cart = await GetOrCreateCartAsync(userId);

        var item = await _context.CartItems
            .FirstOrDefaultAsync(ci => ci.Id == cartItemId && ci.CartId == cart.Id)
            ?? throw new NotFoundException("Cart item not found.");

        return (cart, item);
    }

    private async Task<decimal> GetSubtotalAsync(Guid cartId)
    {
        var lines = await _context.CartItems
            .Where(ci => ci.CartId == cartId)
            .Select(ci => new { ci.Quantity, ci.MenuItem.Price })
            .ToListAsync();

        return lines.Sum(l => l.Quantity * l.Price);
    }

    private async Task<CartResponse> BuildResponseAsync(Guid cartId)
    {
        var cart = await _context.Carts
            .Include(c => c.Coupon)
            .FirstAsync(c => c.Id == cartId);

        var rawItems = await _context.CartItems
            .Where(ci => ci.CartId == cartId)
            .Include(ci => ci.MenuItem)
            .ToListAsync();

        var items = rawItems.Select(ci => new CartItemResponse(
            ci.Id,
            ci.MenuItemId,
            ci.MenuItem.Name,
            ci.Quantity,
            ci.MenuItem.Price,
            ci.Quantity * ci.MenuItem.Price,
            ci.MenuItem.Price != ci.PriceAtAdd,
            ci.MenuItem.IsAvailable && !ci.MenuItem.IsDeleted
        )).ToList();

        var subtotal = items.Sum(i => i.LineTotal);

        AppliedCouponResponse? couponResponse = null;
        decimal discount = 0m;

        if (cart.Coupon is not null)
        {
            discount = CalculateDiscount(cart.Coupon, subtotal);
            couponResponse = new AppliedCouponResponse(cart.Coupon.Id, cart.Coupon.Code, discount);
        }

        string? restaurantName = null;
        if (cart.RestaurantId.HasValue)
        {
            restaurantName = await _context.Restaurants
                .Where(r => r.Id == cart.RestaurantId.Value)
                .Select(r => r.Name)
                .FirstOrDefaultAsync();
        }

        return new CartResponse(
            cart.RestaurantId,
            restaurantName,
            items,
            subtotal,
            couponResponse,
            discount,
            subtotal - discount);
    }

    private static decimal CalculateDiscount(Coupon coupon, decimal subtotal)
    {
        var discount = coupon.DiscountType == DiscountType.Percentage
            ? subtotal * (coupon.DiscountValue / 100m)
            : coupon.DiscountValue;

        if (coupon.MaxDiscountAmount.HasValue)
            discount = Math.Min(discount, coupon.MaxDiscountAmount.Value);

        return Math.Min(discount, subtotal); // discount can never exceed the subtotal
    }
}
