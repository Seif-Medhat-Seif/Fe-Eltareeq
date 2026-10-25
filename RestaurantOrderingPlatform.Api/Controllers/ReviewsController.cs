using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantOrderingPlatform.Application.Interfaces;
using RestaurantOrderingPlatform.Application.DTOs.Reviews;

namespace RestaurantOrderingPlatform.Api.Controllers;

[ApiController]
[Route("api")]
public class ReviewsController : ControllerBase
{
    private readonly IReviewService _reviews;
    public ReviewsController(IReviewService reviews) => _reviews = reviews;

    [HttpPost("orders/{orderId:guid}/review")]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> Create(Guid orderId, CreateReviewRequest request, CancellationToken ct)
    {
        var created = await _reviews.CreateAsync(orderId, request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpGet("restaurants/{restaurantId:guid}/reviews")]
    [AllowAnonymous]
    public async Task<IActionResult> GetByRestaurant(
        Guid restaurantId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => Ok(await _reviews.GetByRestaurantAsync(restaurantId, page, pageSize, ct));

    [HttpGet("reviews/{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => Ok(await _reviews.GetByIdAsync(id, ct));

    [HttpPost("reviews/{id:guid}/reply")]
    [Authorize(Roles = "RestaurantOwner")]
    public async Task<IActionResult> Reply(Guid id, ReplyToReviewRequest request, CancellationToken ct)
        => Ok(await _reviews.ReplyAsync(id, request, ct));

    [HttpPost("reviews/{id:guid}/report")]
    [Authorize(Roles = "RestaurantOwner")]
    public async Task<IActionResult> Report(Guid id, ReportReviewRequest request, CancellationToken ct)
    {
        await _reviews.ReportAsync(id, request, ct);
        return NoContent();
    }
}
