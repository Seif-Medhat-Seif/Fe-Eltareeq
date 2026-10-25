using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantOrderingPlatform.Application.Interfaces;
using RestaurantOrderingPlatform.Application.DTOs.Coupons;

namespace RestaurantOrderingPlatform.Api.Controllers;

[ApiController]
[Route("api/restaurant/coupons")]
[Authorize(Roles = "RestaurantOwner")]   // الموظف (Customer) مالوش صلاحية كوبونات
public class CouponsController : ControllerBase
{
    private readonly ICouponService _coupons;
    public CouponsController(ICouponService coupons) => _coupons = coupons;

    [HttpPost]
    public async Task<IActionResult> Create(CreateCouponRequest request, CancellationToken ct)
    {
        var created = await _coupons.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] bool? isActive, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => Ok(await _coupons.GetAllAsync(isActive, page, pageSize, ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => Ok(await _coupons.GetByIdAsync(id, ct));

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateCouponRequest request, CancellationToken ct)
        => Ok(await _coupons.UpdateAsync(id, request, ct));

    [HttpPatch("{id:guid}/active")]
    public async Task<IActionResult> SetActive(Guid id, SetCouponActiveRequest request, CancellationToken ct)
        => Ok(await _coupons.SetActiveAsync(id, request.IsActive, ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _coupons.DeleteAsync(id, ct);
        return NoContent();
    }
}
