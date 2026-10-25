using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantOrderingPlatform.Application.Interfaces;
using RestaurantOrderingPlatform.Application.DTOs.Drivers;
using RestaurantOrderingPlatform.Domain.Enums;
using RestaurantOrderingPlatform.Application.DTOs;

namespace RestaurantOrderingPlatform.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/drivers")]
[Authorize(Roles = "Admin")]
public class AdminDriversController : ControllerBase
{
    private readonly IDriverAdminService _admin;
    public AdminDriversController(IDriverAdminService admin) => _admin = admin;

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<AdminDriverSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] DriverVerificationStatus? status,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => Ok(await _admin.GetDriversAsync(status, page, pageSize, ct));

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AdminDriverDetailResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => Ok(await _admin.GetDriverAsync(id, ct));

    [HttpPatch("{id:guid}/verification")]
    [ProducesResponseType(typeof(AdminDriverDetailResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Review(
        Guid id, [FromBody] ReviewDriverVerificationRequest request, CancellationToken ct)
        => Ok(await _admin.ReviewVerificationAsync(id, request, ct));
}
