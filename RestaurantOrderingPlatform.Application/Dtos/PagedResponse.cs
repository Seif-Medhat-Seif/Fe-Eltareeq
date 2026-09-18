using System;
using System.Collections.Generic;

namespace RestaurantOrderingPlatform.Application.DTOs;

/// <summary>
/// Generic paged response envelope for list endpoints.
/// </summary>
public record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount)
{
    public int TotalPages =>
        PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
