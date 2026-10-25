using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantOrderingPlatform.Application.Interfaces;
using RestaurantOrderingPlatform.Application.DTOs.Reports;

namespace RestaurantOrderingPlatform.Api.Controllers;

[ApiController]
[Route("api/restaurant/reports")]
[Authorize(Roles = "RestaurantOwner")]   // الموظفين ممنوعين من التقارير
public class ReportsController : ControllerBase
{
    private readonly IReportService _reports;
    public ReportsController(IReportService reports) => _reports = reports;

    [HttpGet("sales-summary")]
    public async Task<IActionResult> SalesSummary([FromQuery] ReportDateRangeQuery query, CancellationToken ct)
        => Ok(await _reports.GetSalesSummaryAsync(query, ct));

    [HttpGet("top-items")]
    public async Task<IActionResult> TopItems([FromQuery] TopItemsQuery query, CancellationToken ct)
        => Ok(await _reports.GetTopItemsAsync(query, ct));

    [HttpGet("order-status-breakdown")]
    public async Task<IActionResult> OrderStatusBreakdown([FromQuery] ReportDateRangeQuery query, CancellationToken ct)
        => Ok(await _reports.GetOrderStatusBreakdownAsync(query, ct));

    [HttpGet("reviews-summary")]
    public async Task<IActionResult> ReviewsSummary([FromQuery] ReviewsDateRangeQuery query, CancellationToken ct)
        => Ok(await _reports.GetReviewsSummaryAsync(query, ct));
}
