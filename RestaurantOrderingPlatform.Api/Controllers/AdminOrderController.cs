using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantOrderingPlatform.Application.DTOs.Order;
using RestaurantOrderingPlatform.Application.Interfaces;

namespace RestaurantOrderingPlatform.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/orders")]
[Authorize(Roles = "Admin")]
public class AdminOrderController : ControllerBase
{
    private readonly IOrderService _orders;
    public AdminOrderController(IOrderService orders) => _orders = orders;

    [HttpGet("payment-mismatches")]
    public async Task<IActionResult> GetMismatches(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => Ok(await _orders.GetPaymentMismatchesAsync(page, pageSize, ct));

    [HttpPatch("payment-mismatches/{id:guid}")]
    public async Task<IActionResult> Resolve(
        Guid id, [FromBody] ResolvePaymentMismatchRequest request, CancellationToken ct)
        => Ok(await _orders.ResolvePaymentMismatchAsync(id, request, ct));
}