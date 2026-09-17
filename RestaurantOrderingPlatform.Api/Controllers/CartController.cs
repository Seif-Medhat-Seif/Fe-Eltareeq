using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantOrderingPlatform.Application.DTOs.Cart;
using RestaurantOrderingPlatform.Application.Interfaces;

namespace RestaurantOrderingPlatform.Api.Controllers;

[ApiController]
[Route("api/cart")]
[Authorize(Roles = "Customer")]
public class CartController : ControllerBase
{
    private readonly ICartService _cartService;

    public CartController(ICartService cartService)
    {
        _cartService = cartService;
    }

    /// <summary>GET /api/cart — live price recalculation; flags priceChanged/isAvailable per item.</summary>
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var result = await _cartService.GetMyCartAsync(CurrentUserId);
        return Ok(result);
    }

    /// <summary>
    /// POST /api/cart/items — adding from a different restaurant returns 409 unless
    /// confirmReplace=true, in which case the cart is replaced.
    /// </summary>
    [HttpPost("items")]
    public async Task<IActionResult> AddItem([FromBody] AddCartItemRequest request)
    {
        var result = await _cartService.AddItemAsync(CurrentUserId, request);
        return Ok(result);
    }

    /// <summary>PUT /api/cart/items/{id} — update quantity (quantity=0 is not allowed; use DELETE).</summary>
    [HttpPut("items/{id:guid}")]
    public async Task<IActionResult> UpdateItem(Guid id, [FromBody] UpdateCartItemRequest request)
    {
        var result = await _cartService.UpdateItemAsync(CurrentUserId, id, request);
        return Ok(result);
    }

    /// <summary>DELETE /api/cart/items/{id} — remove a single line item.</summary>
    [HttpDelete("items/{id:guid}")]
    public async Task<IActionResult> RemoveItem(Guid id)
    {
        var result = await _cartService.RemoveItemAsync(CurrentUserId, id);
        return Ok(result);
    }

    /// <summary>DELETE /api/cart — clear the entire cart (items + coupon).</summary>
    [HttpDelete]
    public async Task<IActionResult> Clear()
    {
        var result = await _cartService.ClearCartAsync(CurrentUserId);
        return Ok(result);
    }

    /// <summary>
    /// POST /api/cart/coupon — validates restaurant match, active, expiry, minimum order.
    /// Usage-limit checks are re-validated atomically at checkout (Module 6).
    /// </summary>
    [HttpPost("coupon")]
    public async Task<IActionResult> ApplyCoupon([FromBody] ApplyCouponRequest request)
    {
        var result = await _cartService.ApplyCouponAsync(CurrentUserId, request);
        return Ok(result);
    }

    /// <summary>DELETE /api/cart/coupon — remove the applied coupon without touching items.</summary>
    [HttpDelete("coupon")]
    public async Task<IActionResult> RemoveCoupon()
    {
        var result = await _cartService.RemoveCouponAsync(CurrentUserId);
        return Ok(result);
    }

    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
