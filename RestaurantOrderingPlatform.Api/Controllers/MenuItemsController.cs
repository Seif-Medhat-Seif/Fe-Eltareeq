using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantOrderingPlatform.Application.DTOs.Menu;
using RestaurantOrderingPlatform.Application.Interfaces;

namespace RestaurantOrderingPlatform.Api.Controllers;

[ApiController]
public class MenuItemsController : ControllerBase
{
    private readonly IMenuService _menuService;

    public MenuItemsController(IMenuService menuService)
    {
        _menuService = menuService;
    }

    /// <summary>POST /api/categories/{categoryId}/items — Owner only.</summary>
    [HttpPost("api/categories/{categoryId:guid}/items")]
    [Authorize(Roles = "RestaurantOwner")]
    public async Task<IActionResult> Create(Guid categoryId, [FromBody] CreateMenuItemRequest request)
    {
        var result = await _menuService.CreateItemAsync(CurrentUserId, categoryId, request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>PUT /api/menu-items/{id} — Owner only. Never retroactively touches OrderItem snapshots.</summary>
    [HttpPut("api/menu-items/{id:guid}")]
    [Authorize(Roles = "RestaurantOwner")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateMenuItemRequest request)
    {
        var result = await _menuService.UpdateItemAsync(CurrentUserId, id, request);
        return Ok(result);
    }

    /// <summary>PATCH /api/menu-items/{id}/availability — Owner only. Fast 86-it toggle.</summary>
    [HttpPatch("api/menu-items/{id:guid}/availability")]
    [Authorize(Roles = "RestaurantOwner")]
    public async Task<IActionResult> SetAvailability(Guid id, [FromBody] SetMenuItemAvailabilityRequest request)
    {
        var result = await _menuService.SetItemAvailabilityAsync(CurrentUserId, id, request);
        return Ok(result);
    }

    /// <summary>DELETE /api/menu-items/{id} — Owner only. Soft delete (IsDeleted=true).</summary>
    [HttpDelete("api/menu-items/{id:guid}")]
    [Authorize(Roles = "RestaurantOwner")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _menuService.DeleteItemAsync(CurrentUserId, id);
        return NoContent();
    }

    // No standalone GET /api/menu-items/{id} endpoint was locked in Phase 5 — items
    // are always read in the context of a category or the full menu. This action
    // exists only as a CreatedAtAction route target; if a direct GET is ever needed,
    // add it as a deliberate, separate Phase 5 addendum rather than inferring it now.
    [NonAction]
    public IActionResult GetById(Guid id) => Ok();

    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
