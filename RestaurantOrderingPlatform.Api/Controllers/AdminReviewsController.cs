using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantOrderingPlatform.Application.Interfaces;
using RestaurantOrderingPlatform.Application.DTOs.Reviews;

namespace RestaurantOrderingPlatform.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/reviews")]
[Authorize(Roles = "Admin")]
public class AdminReviewsController : ControllerBase
{
    private readonly IReviewAdminService _admin;
    public AdminReviewsController(IReviewAdminService admin) => _admin = admin;

    [HttpGet("reported")]
    public async Task<IActionResult> GetReported(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => Ok(await _admin.GetReportedAsync(page, pageSize, ct));

    [HttpPatch("{id:guid}/moderate")]
    public async Task<IActionResult> Moderate(Guid id, ModerateReviewRequest request, CancellationToken ct)
    {
        await _admin.ModerateAsync(id, request, ct);
        return NoContent();
    }
}
