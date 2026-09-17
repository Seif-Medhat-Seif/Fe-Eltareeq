namespace RestaurantOrderingPlatform.Application.DTOs.Cart;

// ---------- Requests ----------

public record AddCartItemRequest(Guid MenuItemId, int Quantity, bool ConfirmReplace = false);

public record UpdateCartItemRequest(int Quantity);

public record ApplyCouponRequest(string Code);

// ---------- Responses ----------

public record CartItemResponse(
    Guid Id,
    Guid MenuItemId,
    string Name,
    int Quantity,
    decimal CurrentUnitPrice,
    decimal LineTotal,
    bool PriceChanged,   // true if CurrentUnitPrice differs from price at time of last add/update
    bool IsAvailable);   // false if the MenuItem is now unavailable or soft-deleted

public record AppliedCouponResponse(
    Guid CouponId,
    string Code,
    decimal DiscountAmount); // computed against the current subtotal, recalculated live like prices

/// <summary>
/// GET /api/cart. Empty-state shape (BR: predictable JSON, never a bare 204):
/// RestaurantId=null, Items=[], Coupon=null when the cart has nothing in it.
/// </summary>
public record CartResponse(
    Guid? RestaurantId,
    string? RestaurantName,
    IReadOnlyList<CartItemResponse> Items,
    decimal Subtotal,
    AppliedCouponResponse? Coupon,
    decimal DiscountAmount,
    decimal Total); // Subtotal - DiscountAmount (delivery fee is added at checkout, not shown here)

/// <summary>
/// Returned as the 409 body when adding an item from a different restaurant without
/// confirmReplace=true — lets the client show "replace your cart?" with context.
/// </summary>
public record CartConflictResponse(
    Guid CurrentRestaurantId,
    string CurrentRestaurantName,
    Guid RequestedRestaurantId,
    string Message);
