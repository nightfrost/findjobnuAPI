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

        public string? LinkedInId { get; set; } // Store LinkedIn ID if needed
        public bool IsLinkedInUser { get; set; } = false; // Flag to indicate if this user is from LinkedIn
        public bool HasVerifiedLinkedIn { get; set; } = false; 
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? LinkedInProfileUrl { get; set; } = string.Empty;
        public string? LinkedInHeadline { get; set; } = string.Empty;
        public DateTime? LastLinkedInSync { get; set; } = null;
    }
}