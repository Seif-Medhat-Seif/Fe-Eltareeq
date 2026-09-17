namespace RestaurantOrderingPlatform.Domain.Entities;

public class Customer
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    // Navigation
    public ICollection<Address> Addresses { get; set; } = new List<Address>();
    public Cart? Cart { get; set; }
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
    public ICollection<RestaurantEmployee> RestaurantEmployeeLinks { get; set; } = new List<RestaurantEmployee>();
}
