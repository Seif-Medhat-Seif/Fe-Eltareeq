using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantOrderingPlatform.Application.DTOs.Customer;
using RestaurantOrderingPlatform.Application.Interfaces;

namespace RestaurantOrderingPlatform.Api.Controllers;

[ApiController]
[Authorize(Roles = "Customer")]
public class AddressesController : ControllerBase
{
    private readonly ICustomerService _customerService;

    public AddressesController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    /// <summary>GET /api/customers/me/addresses</summary>
    [HttpGet("api/customers/me/addresses")]
    public async Task<IActionResult> GetMyAddresses()
    {
        var result = await _customerService.GetMyAddressesAsync(CurrentUserId);
        return Ok(result);
    }

    /// <summary>POST /api/customers/me/addresses — first address added is auto-default.</summary>
    [HttpPost("api/customers/me/addresses")]
    public async Task<IActionResult> Add([FromBody] CreateAddressRequest request)
    {
        var result = await _customerService.AddAddressAsync(CurrentUserId, request);
        return CreatedAtAction(nameof(GetMyAddresses), result);
    }

    /// <summary>PUT /api/addresses/{id} — must own the address.</summary>
    [HttpPut("api/addresses/{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAddressRequest request)
    {
        var result = await _customerService.UpdateAddressAsync(CurrentUserId, id, request);
        return Ok(result);
    }

    /// <summary>DELETE /api/addresses/{id} — auto-promotes another address if this was the default.</summary>
    [HttpDelete("api/addresses/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _customerService.DeleteAddressAsync(CurrentUserId, id);
        return NoContent();
    }

    /// <summary>PATCH /api/addresses/{id}/default — atomically clears default on all other addresses.</summary>
    [HttpPatch("api/addresses/{id:guid}/default")]
    public async Task<IActionResult> SetDefault(Guid id)
    {
        var result = await _customerService.SetDefaultAddressAsync(CurrentUserId, id);
        return Ok(result);
    }

    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
