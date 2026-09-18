using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantOrderingPlatform.Application.DTOs.Order;
using RestaurantOrderingPlatform.Application.Interfaces;
using RestaurantOrderingPlatform.Domain.Enums;

namespace RestaurantOrderingPlatform.Api.Controllers;

[ApiController]
[Route("api/restaurant/orders")]
// ★ Employee بيسجّل كـ Customer (BR: مش role مستقل)، فالـ [Authorize] هنا
//   بيسمح للاتنين، والفلترة الحقيقية بتحصل في
//   OrderService.ResolveRestaurantScopeAsync — ده تطبيق BR-9.
[Authorize(Roles = "RestaurantOwner,Customer")]
public class RestaurantOrderController : ControllerBase
{
    private readonly IOrderService _orders;
    public RestaurantOrderController(IOrderService orders) => _orders = orders;

    [HttpGet]
    public async Task<IActionResult> GetQueue(
        [FromQuery] OrderStatus? status,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => Ok(await _orders.GetRestaurantOrdersAsync(status, page, pageSize, ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => Ok(await _orders.GetRestaurantOrderByIdAsync(id, ct));

    [HttpPatch("{id:guid}/accept")]
    public async Task<IActionResult> Accept(Guid id, CancellationToken ct)
        => Ok(await _orders.AcceptAsync(id, ct));

    [HttpPatch("{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] RejectOrderRequest request, CancellationToken ct)
        => Ok(await _orders.RejectAsync(id, request.Reason, ct));

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(
        Guid id, [FromBody] UpdateOrderStatusRequest request, CancellationToken ct)
        => Ok(await _orders.UpdateStatusAsync(id, request.Status, ct));
}