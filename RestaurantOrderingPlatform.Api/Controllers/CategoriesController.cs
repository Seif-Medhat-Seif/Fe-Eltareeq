using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantOrderingPlatform.Application.DTOs.Menu;
using RestaurantOrderingPlatform.Application.Interfaces;

namespace RestaurantOrderingPlatform.Api.Controllers;

[ApiController]
public class CategoriesController : ControllerBase
{
    private readonly IMenuService _menuService;

    public CategoriesController(IMenuService menuService)
    {
        _menuService = menuService;
    }

    /// <summary>POST /api/restaurants/{restaurantId}/categories — Owner only.</summary>
    [HttpPost("api/restaurants/{restaurantId:guid}/categories")]
    [Authorize(Roles = "RestaurantOwner")]
    public async Task<IActionResult> Create(Guid restaurantId, [FromBody] CreateMenuCategoryRequest request)
    {
        var result = await _menuService.CreateCategoryAsync(CurrentUserId, restaurantId, request);
        return CreatedAtAction(nameof(GetItems), new { categoryId = result.Id }, result);
    }

    /// <summary>GET /api/restaurants/{restaurantId}/categories — Public, lightweight (no items).</summary>
    [HttpGet("api/restaurants/{restaurantId:guid}/categories")]
    [AllowAnonymous]
    public async Task<IActionResult> GetByRestaurant(Guid restaurantId)
    {
        var result = await _menuService.GetCategoriesAsync(restaurantId);
        return Ok(result);
    }

    /// <summary>PUT /api/categories/{id} — Owner only.</summary>
    [HttpPut("api/categories/{id:guid}")]
    [Authorize(Roles = "RestaurantOwner")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateMenuCategoryRequest request)
    {
        var result = await _menuService.UpdateCategoryAsync(CurrentUserId, id, request);
        return Ok(result);
    }

    /// <summary>DELETE /api/categories/{id} — Owner only. 409 if the category still has items.</summary>
    [HttpDelete("api/categories/{id:guid}")]
    [Authorize(Roles = "RestaurantOwner")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _menuService.DeleteCategoryAsync(CurrentUserId, id);
        return NoContent();
    }

    /// <summary>GET /api/categories/{categoryId}/items — Public, items of one category.</summary>
    [HttpGet("api/categories/{categoryId:guid}/items")]
    [AllowAnonymous]
    public async Task<IActionResult> GetItems(Guid categoryId)
    {
        var result = await _menuService.GetItemsByCategoryAsync(categoryId);
        return Ok(result);
    }

    // Matches the same claim extraction pattern used across Module 1/2 controllers.
    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
