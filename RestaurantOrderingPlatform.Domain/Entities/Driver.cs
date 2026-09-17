using RestaurantOrderingPlatform.Domain.Enums;

namespace RestaurantOrderingPlatform.Domain.Entities;

public class Driver
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }
    public string NationalID { get; set; } = string.Empty;

    public VehicleType VehicleType { get; set; }
    public string VehicleImageUrl { get; set; } = string.Empty;
    public string? VehiclePlateNumber { get; set; }

    // Navigation
    public ICollection<Order> Deliveries { get; set; } = new List<Order>();
}
