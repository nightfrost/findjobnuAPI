namespace JobAgentWorkerService.Models
{
    public class AspNetUser
    {
        public string Id { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string? LinkedInId { get; set; }
        public bool IsLinkedInUser { get; set; }
        public bool HasVerifiedLinkedIn { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? LinkedInProfileUrl { get; set; }
        public string? LinkedInHeadline { get; set; }
        public DateTime? LastLinkedInSync { get; set; }
        public string? UserName { get; set; }
        public string? NormalizedUserName { get; set; }
        public string? Email { get; set; }
        public string? NormalizedEmail { get; set; }
        public bool EmailConfirmed { get; set; }
        public string? PasswordHash { get; set; }
        public string? SecurityStamp { get; set; }
        public string? ConcurrencyStamp { get; set; }
        public string? PhoneNumber { get; set; }
        public bool PhoneNumberConfirmed { get; set; }
        public bool TwoFactorEnabled { get; set; }
        public DateTimeOffset? LockoutEnd { get; set; }
        public bool LockoutEnabled { get; set; }
        public int AccessFailedCount { get; set; }
    }
}
