using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantOrderingPlatform.Application.Interfaces;
using RestaurantOrderingPlatform.Application.DTOs.Orders;

namespace RestaurantOrderingPlatform.Api.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize(Roles = "Customer")]
public class OrderController : ControllerBase
{
    private readonly IOrderService _orders;
    public OrderController(IOrderService orders) => _orders = orders;

    [HttpPost]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Checkout([FromBody] CreateOrderRequest request, CancellationToken ct)
    {
        var order = await _orders.CheckoutAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
    }

    [HttpPost("{id:guid}/payment")]
    [ProducesResponseType(typeof(PaymentIntentResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> InitiatePayment(Guid id, CancellationToken ct)
        => Ok(await _orders.InitiatePaymentAsync(id, ct));

    [HttpGet]
    public async Task<IActionResult> GetMine(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => Ok(await _orders.GetMyOrdersAsync(page, pageSize, ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => Ok(await _orders.GetMyOrderByIdAsync(id, ct));

    /// <summary>إلغاء أوردر لسه متدفعش. (إضافة على الـ spec — الشرح في README)</summary>
    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        await _orders.CancelAsync(id, ct);
        return NoContent();
    }
}