using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantOrderingPlatform.Application.DTOs.Auth;
using RestaurantOrderingPlatform.Application.Interfaces;

namespace RestaurantOrderingPlatform.Api.Controllers;

[ApiController]
[Route("api/restaurant-owner")]
[Authorize(Roles = "RestaurantOwner")]
public class RestaurantOwnerController : ControllerBase
{
    private readonly IRestaurantOwnerService _restaurantOwnerService;

    public RestaurantOwnerController(IRestaurantOwnerService restaurantOwnerService)
    {
        _restaurantOwnerService = restaurantOwnerService;
    }

    [HttpPost("employees")]
    public async Task<IActionResult> AssignEmployee([FromBody] AssignEmployeeRequest request, CancellationToken ct)
    {
        await _restaurantOwnerService.AssignEmployeeAsync(GetCurrentUserId(), request, ct);
        return Ok(new { message = "Employee assigned." });
    }

    [HttpDelete("employees/{employeeId:guid}")]
    public async Task<IActionResult> RemoveEmployee(Guid employeeId, CancellationToken ct)
    {
        await _restaurantOwnerService.RemoveEmployeeAsync(GetCurrentUserId(), employeeId, ct);
        return NoContent();
    }

    private Guid GetCurrentUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.Parse(sub!); // [Authorize] guarantees this claim exists — a missing/malformed sub is a 500, correctly, not a 401
    }
}
