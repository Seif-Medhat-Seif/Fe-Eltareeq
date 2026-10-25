using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantOrderingPlatform.Application.Interfaces;
using RestaurantOrderingPlatform.Application.DTOs.Drivers;
using RestaurantOrderingPlatform.Application.DTOs;

namespace RestaurantOrderingPlatform.Api.Controllers;

[ApiController]
[Route("api/driver/deliveries")]
[Authorize(Roles = "Driver")]
public class DriverDeliveriesController : ControllerBase
{
    private readonly IDriverService _drivers;
    public DriverDeliveriesController(IDriverService drivers) => _drivers = drivers;

    /// <summary>Polling endpoint: أوردرات Ready من غير سواق. للسواق المعتمد (Approved) بس.</summary>
    [HttpGet("available")]
    [ProducesResponseType(typeof(PagedResponse<AvailableDeliveryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAvailable(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => Ok(await _drivers.GetAvailableAsync(page, pageSize, ct));

    /// <summary>409 لو عندك توصيلة نشطة، أو سواق تاني سبقك، أو حسابك مش معتمد.</summary>
    [HttpPost("{id:guid}/accept")]
    [ProducesResponseType(typeof(DeliveryResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Accept(Guid id, CancellationToken ct)
        => Ok(await _drivers.AcceptAsync(id, ct));

    /// <summary>{ "delivery": null } لو مفيش توصيلة نشطة.</summary>
    [HttpGet("current")]
    [ProducesResponseType(typeof(CurrentDeliveryResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCurrent(CancellationToken ct)
        => Ok(await _drivers.GetCurrentAsync(ct));

    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(DeliveryResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateStatus(
        Guid id, [FromBody] UpdateDeliveryStatusRequest request, CancellationToken ct)
        => Ok(await _drivers.UpdateStatusAsync(id, request.Status, ct));

    [HttpGet("history")]
    [ProducesResponseType(typeof(PagedResponse<DeliverySummaryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHistory(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => Ok(await _drivers.GetHistoryAsync(page, pageSize, ct));
}
