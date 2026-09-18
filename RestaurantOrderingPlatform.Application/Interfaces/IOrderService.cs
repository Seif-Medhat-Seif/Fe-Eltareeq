using RestaurantOrderingPlatform.Application.DTOs;
using RestaurantOrderingPlatform.Application.DTOs.Order;
using RestaurantOrderingPlatform.Domain.Enums;

namespace RestaurantOrderingPlatform.Application.Interfaces;

public interface IOrderService
{
    // Customer
    Task<OrderResponse> CheckoutAsync(CreateOrderRequest request, CancellationToken ct = default);
    Task<PaymentIntentResponse> InitiatePaymentAsync(Guid orderId, CancellationToken ct = default);
    Task<PagedResponse<OrderSummaryResponse>> GetMyOrdersAsync(int page, int pageSize, CancellationToken ct = default);
    Task<OrderResponse> GetMyOrderByIdAsync(Guid orderId, CancellationToken ct = default);
    Task CancelAsync(Guid orderId, CancellationToken ct = default);

    // Restaurant (Owner / Employee)
    Task<PagedResponse<RestaurantOrderSummaryResponse>> GetRestaurantOrdersAsync(
        OrderStatus? status, int page, int pageSize, CancellationToken ct = default);
    Task<OrderResponse> GetRestaurantOrderByIdAsync(Guid orderId, CancellationToken ct = default);
    Task<OrderResponse> AcceptAsync(Guid orderId, CancellationToken ct = default);
    Task<OrderResponse> RejectAsync(Guid orderId, string reason, CancellationToken ct = default);
    Task<OrderResponse> UpdateStatusAsync(Guid orderId, OrderStatus newStatus, CancellationToken ct = default);

    // Admin
    Task<PagedResponse<PaymentMismatchResponse>> GetPaymentMismatchesAsync(int page, int pageSize, CancellationToken ct = default);
    Task<OrderResponse> ResolvePaymentMismatchAsync(Guid orderId, ResolvePaymentMismatchRequest request, CancellationToken ct = default);
}