using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace findjobnuAPI.Models
{
    /// <summary>
    /// Result of Work profile import operation (formerly LinkedInProfileResult)
    /// </summary>
    public class LinkedInProfileResult
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        public string? UserId { get; set; }
        public WorkProfile? Profile { get; set; } // Renamed from LinkedInProfile
    }

    /// <summary>
    /// Work profile data (formerly LinkedInProfile)
    /// </summary>
    public class WorkProfile
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public int UserProfileId { get; set; }
        [Required]
        public UserProfile UserProfile { get; set; } = null!;
        public BasicInfo? BasicInfo { get; set; }
        public ICollection<Experience>? Experiences { get; set; }
        public ICollection<Education>? Educations { get; set; }
        public ICollection<Interest>? Interests { get; set; }
        public ICollection<Accomplishment>? Accomplishments { get; set; }
        public ICollection<Contact>? Contacts { get; set; }
    }

    /// <summary>
    /// Basic profile information
    /// </summary>
    [Owned]
    public class BasicInfo
    {
        public string? Name { get; set; }
        public string? About { get; set; }
        public string? Location { get; set; }
        public string? Company { get; set; }
        public string? JobTitle { get; set; }
        public string? LinkedinUrl { get; set; }
        public bool OpenToWork { get; set; }
    }

    // Additional classes for profile sections
    public class Experience
    {
        [Key]
        public int Id { get; set; }
        public string? PositionTitle { get; set; }
        public string? Company { get; set; }
        public string? FromDate { get; set; }
        public string? ToDate { get; set; }
        public string? Duration { get; set; }
        public string? Location { get; set; }
        public string? Description { get; set; }
        public string? LinkedinUrl { get; set; }
        public int WorkProfileId { get; set; } // Renamed from LinkedInProfileId
        public WorkProfile? WorkProfile { get; set; } // Renamed from LinkedInProfile
    }

    public class Education
    {
        [Key]
        public int Id { get; set; }
        public string? Institution { get; set; }
        public string? Degree { get; set; }
        public string? FromDate { get; set; }
        public string? ToDate { get; set; }
        public string? Description { get; set; }
        public string? LinkedinUrl { get; set; }
        public int WorkProfileId { get; set; } // Renamed from LinkedInProfileId
        public WorkProfile? WorkProfile { get; set; } // Renamed from LinkedInProfile
    }

    public class Interest
    {
        [Key]
        public int Id { get; set; }
        public string? Title { get; set; }
        public int WorkProfileId { get; set; } // Renamed from LinkedInProfileId
        public WorkProfile? WorkProfile { get; set; } // Renamed from LinkedInProfile
    }

    public class Accomplishment
    {
        [Key]
        public int Id { get; set; }
        public string? Category { get; set; }
        public string? Title { get; set; }
        public int WorkProfileId { get; set; } // Renamed from LinkedInProfileId
        public WorkProfile? WorkProfile { get; set; } // Renamed from LinkedInProfile
    }

    public class Contact
    {
        [Key]
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Occupation { get; set; }
        public string? Url { get; set; }
        public int WorkProfileId { get; set; } // Renamed from LinkedInProfileId
        public WorkProfile? WorkProfile { get; set; } // Renamed from LinkedInProfile
    }
}
