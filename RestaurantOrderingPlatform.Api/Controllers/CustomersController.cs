using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantOrderingPlatform.Application.DTOs.Customer;
using RestaurantOrderingPlatform.Application.Interfaces;

namespace RestaurantOrderingPlatform.Api.Controllers;

[ApiController]
[Route("api/customers/me")]
[Authorize(Roles = "Customer")]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;

    public CustomersController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    /// <summary>GET /api/customers/me — name/phone only in V1; email is read-only.</summary>
    [HttpGet]
    public async Task<IActionResult> GetMyProfile()
    {
        var result = await _customerService.GetMyProfileAsync(CurrentUserId);
        return Ok(result);
    }

    /// <summary>PUT /api/customers/me — updates FullName/PhoneNumber via IIdentityService.</summary>
    [HttpPut]
    public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateCustomerProfileRequest request)
    {
        var result = await _customerService.UpdateMyProfileAsync(CurrentUserId, request);
        return Ok(result);
    }

    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
