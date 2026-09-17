using RestaurantOrderingPlatform.Application.DTOs.Cart;

namespace RestaurantOrderingPlatform.Application.Interfaces;

public interface ICartService
{
    Task<CartResponse> GetMyCartAsync(Guid userId);

    /// <summary>
    /// Throws ConflictException (mapped to 409) carrying a CartConflictResponse payload
    /// when the cart already has a different restaurant's items and confirmReplace=false.
    /// </summary>
    Task<CartResponse> AddItemAsync(Guid userId, AddCartItemRequest request);

    Task<CartResponse> UpdateItemAsync(Guid userId, Guid cartItemId, UpdateCartItemRequest request);
    Task<CartResponse> RemoveItemAsync(Guid userId, Guid cartItemId);
    Task<CartResponse> ClearCartAsync(Guid userId);

    /// <summary>
    /// Applies partial validation only (restaurant match, active, not expired, minimum
    /// order amount). Usage-limit checks are deliberately deferred to checkout (Module 6)
    /// — see Module 5 handoff note.
    /// </summary>
    Task<CartResponse> ApplyCouponAsync(Guid userId, ApplyCouponRequest request);

    Task<CartResponse> RemoveCouponAsync(Guid userId);
}
