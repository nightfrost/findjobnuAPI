using System.ComponentModel.DataAnnotations;

namespace findjobnuAPI.Models
{
    public class UserProfile
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string UserId { get; set; } = string.Empty; //Foreign key to the user in the authentication service
        [Required, StringLength(50)]
        public string FirstName { get; set; } = string.Empty;
        [Required, StringLength(100)]
        public string LastName { get; set; } = string.Empty;
        public DateTime? DateOfBirth { get; set; }
        [Phone, StringLength(100)]
        public string? PhoneNumber { get; set; } = string.Empty;
        public string? Address { get; set; } = string.Empty;
        public string? City { get; set; } = string.Empty;
        public DateTime? LastUpdatedAt { get; set; }
        public List<string>? SavedJobPosts { get; set; } = [];
        
        // LinkedIn Integration
        public LinkedInProfile? LinkedInProfile { get; set; }
        public bool HasLinkedInConnected => LinkedInProfile != null && !string.IsNullOrEmpty(LinkedInProfile.LinkedInId);
    }
}
