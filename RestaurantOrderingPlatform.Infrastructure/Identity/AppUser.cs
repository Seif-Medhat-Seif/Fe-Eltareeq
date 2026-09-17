using Microsoft.AspNetCore.Identity;

namespace RestaurantOrderingPlatform.Infrastructure.Identity
{
    public class AppUser : IdentityUser<Guid>
    {
        public string FullName { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

    }
}
