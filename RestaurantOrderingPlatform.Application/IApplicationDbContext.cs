using Microsoft.EntityFrameworkCore;
using RestaurantOrderingPlatform.Domain.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace RestaurantOrderingPlatform.Application
{
    public interface IApplicationDbContext
    {
        DbSet<Customer> Customers { get; }
        DbSet<RestaurantOwner> RestaurantOwners { get; }
        DbSet<Driver> Drivers { get; }
        DbSet<RefreshToken> RefreshTokens { get; }

        DbSet<Address> Addresses { get; }
        DbSet<Restaurant> Restaurants { get; }
        DbSet<WorkingHour> WorkingHours { get; }
        DbSet<MenuCategory> MenuCategories { get; }
        DbSet<MenuItem> MenuItems { get; }
        DbSet<RestaurantEmployee> RestaurantEmployees { get; }

        DbSet<Coupon> Coupons { get; }
        DbSet<CouponUsage> CouponUsages { get; }

        DbSet<Cart> Carts { get; }
        DbSet<CartItem> CartItems { get; }

        DbSet<Order> Orders { get; }
        DbSet<OrderItem> OrderItems { get; }
        DbSet<OrderStatusHistory> OrderStatusHistories { get; }
        DbSet<Payment> Payments { get; }

        DbSet<Review> Reviews { get; }
        DbSet<ReviewReport> ReviewReports { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}