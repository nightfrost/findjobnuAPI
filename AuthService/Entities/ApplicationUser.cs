using Microsoft.AspNetCore.Identity;

namespace AuthService.Entities
{
    // Extend IdentityUser to add custom properties if needed.
    // For now, we just inherit to make it compatible with IdentityDbContext.
    public class ApplicationUser : IdentityUser
    {
        // Example: Add a creation date or last login date
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property for RefreshTokens
        public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    }
}