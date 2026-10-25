using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantOrderingPlatform.Application.Interfaces;
using RestaurantOrderingPlatform.Application.DTOs.Drivers;

namespace RestaurantOrderingPlatform.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/deliveries")]
[Authorize(Roles = "Admin")]
public class AdminDeliveriesController : ControllerBase
{
    private readonly IDriverAdminService _admin;
    public AdminDeliveriesController(IDriverAdminService admin) => _admin = admin;

    /// <summary>بيرجّع توصيلة عالقة (قبل PickedUp) لقايمة المتاح ويحرر السواق.</summary>
    [HttpPost("{orderId:guid}/release")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Release(
        Guid orderId, [FromBody] ReleaseDeliveryRequest request, CancellationToken ct)
    {
        await _admin.ReleaseDeliveryAsync(orderId, request.Reason, ct);
        return NoContent();
    }
}
