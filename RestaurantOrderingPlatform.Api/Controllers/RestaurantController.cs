using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantOrderingPlatform.Application.DTOs.Restaurant;
using RestaurantOrderingPlatform.Application.Interfaces;

namespace RestaurantOrderingPlatform.Api.Controllers;

[ApiController]
[Route("api/restaurants")]
public class RestaurantController : ControllerBase
{
    private readonly IRestaurantService _restaurantService;

    public RestaurantController(IRestaurantService restaurantService)
    {
        _restaurantService = restaurantService;
    }

    [HttpPost]
    [Authorize(Roles = "RestaurantOwner")]
    public async Task<ActionResult<RestaurantResponse>> Create([FromBody] CreateRestaurantRequest request, CancellationToken ct)
    {
        var result = await _restaurantService.CreateAsync(GetCurrentUserId(), request, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet("mine")]
    [Authorize(Roles = "RestaurantOwner")]
    public async Task<IActionResult> GetMine(CancellationToken ct)
    {
        var result = await _restaurantService.GetMineAsync(GetCurrentUserId(), ct);
        // Predictable empty-state shape (BR-10) — never a bare 404 for "no restaurant yet".
        return Ok(new { restaurant = result });
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<RestaurantResponse>> GetById(Guid id, CancellationToken ct)
    {
        var result = await _restaurantService.GetByIdAsync(id, ct);
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "RestaurantOwner")]
    public async Task<ActionResult<RestaurantResponse>> Update(Guid id, [FromBody] UpdateRestaurantRequest request, CancellationToken ct)
    {
        var result = await _restaurantService.UpdateAsync(GetCurrentUserId(), id, request, ct);
        return Ok(result);
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = "RestaurantOwner")]
    public async Task<ActionResult<RestaurantResponse>> UpdateStatus(Guid id, [FromBody] UpdateRestaurantStatusRequest request, CancellationToken ct)
    {
        var result = await _restaurantService.UpdateStatusAsync(GetCurrentUserId(), id, request, ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}/working-hours")]
    [AllowAnonymous]
    public async Task<ActionResult<List<WorkingHourDto>>> GetWorkingHours(Guid id, CancellationToken ct)
    {
        var result = await _restaurantService.GetWorkingHoursAsync(id, ct);
        return Ok(result);
    }

    [HttpPut("{id:guid}/working-hours")]
    [Authorize(Roles = "RestaurantOwner")]
    public async Task<ActionResult<List<WorkingHourDto>>> UpdateWorkingHours(Guid id, [FromBody] UpdateWorkingHoursRequest request, CancellationToken ct)
    {
        var result = await _restaurantService.UpdateWorkingHoursAsync(GetCurrentUserId(), id, request, ct);
        return Ok(result);
    }

    private Guid GetCurrentUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.Parse(sub!);
    }
}
