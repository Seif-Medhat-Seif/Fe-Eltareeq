using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RestaurantOrderingPlatform.Application;
using RestaurantOrderingPlatform.Domain.Entities;
using RestaurantOrderingPlatform.Infrastructure.Identity;

namespace RestaurantOrderingPlatform.Infrastructure
{
    public class ApplicationDbContext : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>, IApplicationDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<RestaurantOwner> RestaurantOwners => Set<RestaurantOwner>();
        public DbSet<Driver> Drivers => Set<Driver>();
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

        public DbSet<Address> Addresses => Set<Address>();
        public DbSet<Restaurant> Restaurants => Set<Restaurant>();
        public DbSet<WorkingHour> WorkingHours => Set<WorkingHour>();
        public DbSet<MenuCategory> MenuCategories => Set<MenuCategory>();
        public DbSet<MenuItem> MenuItems => Set<MenuItem>();
        public DbSet<RestaurantEmployee> RestaurantEmployees => Set<RestaurantEmployee>();

        public DbSet<Coupon> Coupons => Set<Coupon>();
        public DbSet<CouponUsage> CouponUsages => Set<CouponUsage>();

        public DbSet<Cart> Carts => Set<Cart>();
        public DbSet<CartItem> CartItems => Set<CartItem>();

        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderItem> OrderItems => Set<OrderItem>();
        public DbSet<OrderStatusHistory> OrderStatusHistories => Set<OrderStatusHistory>();
        public DbSet<Payment> Payments => Set<Payment>();
        public DbSet<ProcessedWebhook> ProcessedWebhooks => Set<ProcessedWebhook>();

        public DbSet<Review> Reviews => Set<Review>();
        public DbSet<ReviewReport> ReviewReports => Set<ReviewReport>();

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            base.ConfigureConventions(configurationBuilder);

            // Every decimal in this schema is money. 18,2 covers EGP amounts with
            // room to spare, and setting it here means no property is ever left
            // on SQL Server's imprecise default (decimal(18,2) isn't guaranteed
            // unless set explicitly per EF Core's docs).
            configurationBuilder.Properties<decimal>().HavePrecision(18, 2);
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder); // MUST run first — sets up Identity's own tables

            builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly); // Auto-discovers
                                                                                            // every IEntityTypeConfiguration<T> class in the project
        }
    }
}
