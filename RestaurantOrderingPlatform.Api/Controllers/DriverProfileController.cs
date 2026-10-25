using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantOrderingPlatform.Application.Interfaces;
using RestaurantOrderingPlatform.Application.DTOs.Drivers;

namespace RestaurantOrderingPlatform.Api.Controllers;

[ApiController]
[Route("api/driver/me")]
[Authorize(Roles = "Driver")]
public class DriverProfileController : ControllerBase
{
    private readonly IDriverService _drivers;
    public DriverProfileController(IDriverService drivers) => _drivers = drivers;

    [HttpGet]
    [ProducesResponseType(typeof(DriverProfileResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(CancellationToken ct)
        => Ok(await _drivers.GetProfileAsync(ct));

    /// <summary>بيبعت بيانات التحقق (أول مرة، أو بعد رفض). بيحوّل الحالة لـ Pending.</summary>
    [HttpPut]
    [ProducesResponseType(typeof(DriverProfileResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Submit([FromBody] UpdateDriverProfileRequest request, CancellationToken ct)
        => Ok(await _drivers.SubmitProfileAsync(request, ct));
}
