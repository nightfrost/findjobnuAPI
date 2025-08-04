namespace AuthService.Models
{
    public class UserInformationDTO
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public bool IsLinkedInUser { get; set; } = false;
        public bool HasVerifiedLinkedIn { get; set; } = false;
        public bool IsEmailConfirmed { get; set; } = false;
        public string? LinkedInId { get; set; } = string.Empty;
        public string? LinkedInProfileUrl { get; set; } = string.Empty;
        public string? LinkedInHeadline { get; set; } = string.Empty;
        public DateTime? LastLinkedInSync { get; set; } = null;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
