namespace RestaurantOrderingPlatform.Domain.Entities;

public class Address
{
    public Guid Id { get; set; }

    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public string Label { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string? Building { get; set; }
    public string? Floor { get; set; }
    public string? Apartment { get; set; }
    public string? Landmark { get; set; }
    public DateTime CreatedAt { get; set; }

    public bool IsDefault { get; set; }
}
