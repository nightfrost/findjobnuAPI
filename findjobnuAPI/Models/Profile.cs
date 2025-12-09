using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace FindjobnuService.Models
{
    /// <summary>
    /// Combined profile data (merges UserProfile and WorkProfile)
    /// </summary>
    public class Profile
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string UserId { get; set; } = string.Empty;
        public DateTime? LastUpdatedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public List<string>? SavedJobPosts { get; set; } = new List<string>();
        public List<string>? Keywords { get; set; } = new List<string>();
        [Required]
        public BasicInfo BasicInfo { get; set; } = new BasicInfo();
        public ICollection<Experience>? Experiences { get; set; } = new List<Experience>();
        public ICollection<Education>? Educations { get; set; } = new List<Education>();
        public ICollection<Interest>? Interests { get; set; } = new List<Interest>();
        public ICollection<Accomplishment>? Accomplishments { get; set; } = new List<Accomplishment>();
        public ICollection<Contact>? Contacts { get; set; } = new List<Contact>();
        public ICollection<Skill>? Skills { get; set; } = new List<Skill>();
        public bool HasJobAgent { get; set; } = false;
        public JobAgent? JobAgent { get; set; }
    }

    [Owned]
    public class BasicInfo
    {
        [Required, StringLength(50)]
        public string FirstName { get; set; } = string.Empty;
        [Required, StringLength(100)]
        public string LastName { get; set; } = string.Empty;
        public DateTime? DateOfBirth { get; set; }
        [Phone, StringLength(100)]
        public string? PhoneNumber { get; set; }
        public string? About { get; set; }
        public string? Location { get; set; }
        public string? Company { get; set; }
        public string? JobTitle { get; set; }
        public string? LinkedinUrl { get; set; }
        public bool OpenToWork { get; set; } = false;
    }

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
        public int ProfileId { get; set; }
        public Profile? Profile { get; set; }
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
        public int ProfileId { get; set; }
        public Profile? Profile { get; set; }
    }

    public class Interest
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string Title { get; set; } = null!;
        public int ProfileId { get; set; }
        public Profile? Profile { get; set; }
    }

    public class Accomplishment
    {
        [Key]
        public int Id { get; set; }
        public string? Category { get; set; }
        public string? Title { get; set; }
        public int ProfileId { get; set; }
        public Profile? Profile { get; set; }
    }

    public class Contact
    {
        [Key]
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Occupation { get; set; }
        public string? Url { get; set; }
        public int ProfileId { get; set; }
        public Profile? Profile { get; set; }
    }

    public enum SkillProficiency
    {
        Beginner,
        Intermediate,
        Advanced,
        Expert
    }

    public class Skill
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string Name { get; set; } = null!;
        [Required]
        public SkillProficiency Proficiency { get; set; }
        public int ProfileId { get; set; }
        public Profile? Profile { get; set; }
    }

    /// <summary>
    /// Result of LinkedIn profile import operation (formerly LinkedInProfileResult)
    /// </summary>
    public class LinkedInProfileResult
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        public string? UserId { get; set; }
        public Profile? Profile { get; set; } // Now uses Profile
    }
}
