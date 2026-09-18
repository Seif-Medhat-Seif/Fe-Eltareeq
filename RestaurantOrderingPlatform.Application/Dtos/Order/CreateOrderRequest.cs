using System;

namespace RestaurantOrderingPlatform.Application.DTOs.Order;

public record CreateOrderRequest(Guid AddressId, string? Notes);
