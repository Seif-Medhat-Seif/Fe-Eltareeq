using RestaurantOrderingPlatform.Domain.Enums;

namespace RestaurantOrderingPlatform.Application.DTOs.Order;

public record UpdateOrderStatusRequest(OrderStatus Status);
