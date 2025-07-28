using System.ComponentModel.DataAnnotations;

namespace findjobnuAPI.Models
{
    public class LinkedInProfile
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string UserProfileId { get; set; } = string.Empty; // FK to UserProfile.UserId
        public string? LinkedInId { get; set; }
        public string? LinkedInProfileUrl { get; set; }
        public string? Summary { get; set; }
        public string? Headline { get; set; }
        public string? Industry { get; set; }
        public string? Location { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public List<string>? Skills { get; set; } = [];
        public DateTime? LastSyncedAt { get; set; }
        public string? AccessToken { get; set; } // Encrypted token for API calls
        public DateTime? TokenExpiresAt { get; set; }
        public string? RefreshToken { get; set; } // Encrypted refresh token
    }

    public class LinkedInExperience
    {
        [Key]
        public int Id { get; set; } 
        public string? CompanyName { get; set; }
        public string? JobTitle { get; set; }
        public string? Description { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsCurrent { get; set; }
        public string? Location { get; set; }
        public int LinkedInProfileId { get; set; } // FK to LinkedInProfile.Id
    }

    public class LinkedInEducation
    {
        [Key]
        public int Id { get; set; } // Primary key for education record
        public string? SchoolName { get; set; }
        public string? Degree { get; set; }
        public string? FieldOfStudy { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Description { get; set; }
        public int LinkedInProfileId { get; set; } // FK to LinkedInProfile.Id
    }
}