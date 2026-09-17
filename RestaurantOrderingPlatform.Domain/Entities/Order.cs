using RestaurantOrderingPlatform.Domain.Enums;

namespace RestaurantOrderingPlatform.Domain.Entities;

public class Order
{
    public Guid Id { get; set; }

    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public Guid RestaurantId { get; set; }
    public Restaurant Restaurant { get; set; } = null!;

    public Guid AddressId { get; set; }
    public Address Address { get; set; } = null!;

    // Nullable — unassigned until a driver accepts (Module 7's atomic accept
    // writes DriverId here under a WHERE DriverId IS NULL guard).
    public Guid? DriverId { get; set; }
    public Driver? Driver { get; set; }

    public Guid? CouponId { get; set; }
    public Coupon? Coupon { get; set; }

    public OrderStatus Status { get; set; }

    public decimal Subtotal { get; set; }
    public decimal DeliveryFee { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal Total { get; set; }

    public DateTime CreatedAt { get; set; }

    // Navigation
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    public ICollection<OrderStatusHistory> StatusHistory { get; set; } = new List<OrderStatusHistory>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public Review? Review { get; set; }
}
