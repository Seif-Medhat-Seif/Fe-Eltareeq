using RestaurantOrderingPlatform.Application.DTOs.Orders;
using RestaurantOrderingPlatform.Domain.Enums;

namespace RestaurantOrderingPlatform.Application.DTOs.Drivers;

public record AdminDriverDetailResponse(
    Guid DriverId,
    string FullName,
    string Email,
    string? PhoneNumber,
    string? NationalId,
    VehicleType VehicleType,
    string? VehiclePlateNumber,
    string? VehicleImageUrl,
    DriverVerificationStatus VerificationStatus,
    DateTime? SubmittedAt,
    DateTime? ReviewedAt,
    string? RejectionReason);

public record AdminDriverSummaryResponse(
    Guid DriverId,
    string FullName,
    string Email,
    string? PhoneNumber,
    VehicleType VehicleType,
    DriverVerificationStatus VerificationStatus,
    DateTime? SubmittedAt);

/// <summary>
/// قايمة المتاح. عن قصد مفيهاش شارع/اسم/تليفون العميل ولا إجمالي الأوردر؛
/// بيتكشفوا بعد القبول بس (GET current).
/// </summary>
public record AvailableDeliveryResponse(
    Guid OrderId,
    string OrderNumber,
    string RestaurantName,
    string RestaurantStreet,
    string RestaurantCity,
    string DeliveryCity,
    int ItemsCount,
    decimal DeliveryFee,
    DateTime ReadyAt);

/// <summary>العقد في الـ spec: { "delivery": null } لو مفيش توصيلة نشطة.</summary>
public record CurrentDeliveryResponse(DeliveryResponse? Delivery);

public record DeliveryCustomerResponse(string Name, string? Phone);

public record DeliveryItemResponse(string Name, int Quantity);

/// <summary>التفاصيل الكاملة. بتتبعت للسواق اللي ماسك الأوردر بس.</summary>
public record DeliveryResponse(
    Guid OrderId,
    string OrderNumber,
    OrderStatus Status,
    DeliveryRestaurantResponse Restaurant,
    DeliveryCustomerResponse Customer,
    OrderAddressResponse Address,
    IReadOnlyList<DeliveryItemResponse> Items);

public record DeliveryRestaurantResponse(string Name, string Street, string City);

public record DeliverySummaryResponse(
    Guid OrderId,
    string OrderNumber,
    string RestaurantName,
    string DeliveryCity,
    decimal DeliveryFee,
    DateTime? DeliveredAt);

public record DriverProfileResponse(
    Guid DriverId,
    string FullName,
    string? PhoneNumber,
    string? NationalId,
    VehicleType VehicleType,
    string? VehiclePlateNumber,
    string? VehicleImageUrl,
    DriverVerificationStatus VerificationStatus,
    string? RejectionReason);


public enum DriverVerificationDecision
{
    Approve,
    Reject
}

public record ReleaseDeliveryRequest(string Reason);

public record ReviewDriverVerificationRequest(DriverVerificationDecision Decision, string? Reason);

/// <summary>المسموح: PickedUp → OnTheWay → Delivered بالترتيب.</summary>
public record UpdateDeliveryStatusRequest(OrderStatus Status);

public record UpdateDriverProfileRequest(
    string NationalId,
    VehicleType VehicleType,
    string? VehiclePlateNumber,
    string VehicleImageUrl);
