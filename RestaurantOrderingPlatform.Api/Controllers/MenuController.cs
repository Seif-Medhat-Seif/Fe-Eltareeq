using Microsoft.AspNetCore.Mvc;
using RestaurantOrderingPlatform.Application.Interfaces;

namespace RestaurantOrderingPlatform.Api.Controllers;

[ApiController]
[Route("api/restaurants/{restaurantId:guid}/menu")]
public class MenuController : ControllerBase
{
    private readonly IMenuService _menuService;

    public MenuController(IMenuService menuService)
    {
        _menuService = menuService;
    }

    /// <summary>
    /// GET /api/restaurants/{restaurantId}/menu
    /// Public. Full menu in one call: categories WITH nested items.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetFullMenu(Guid restaurantId)
    {
        var result = await _menuService.GetFullMenuAsync(restaurantId);
        return Ok(result);
    }
}
