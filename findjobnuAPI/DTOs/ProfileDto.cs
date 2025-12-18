namespace FindjobnuService.DTOs
{
    public class ProfileDto
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public DateTime? LastUpdatedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<string>? SavedJobPosts { get; set; }
        public List<string>? Keywords { get; set; }
        public BasicInfoDto BasicInfo { get; set; } = new BasicInfoDto();
        public List<ExperienceDto>? Experiences { get; set; }
        public List<EducationDto>? Educations { get; set; }
        public List<InterestDto>? Interests { get; set; }
        public List<AccomplishmentDto>? Accomplishments { get; set; }
        public List<ContactDto>? Contacts { get; set; }
        public List<SkillDto>? Skills { get; set; }
        // Indicates whether a job agent has been set up
        public bool HasJobAgent { get; set; }
        // Optional job agent details
        public JobAgentDto? JobAgent { get; set; }
    }

    public class BasicInfoDto
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public DateTime? DateOfBirth { get; set; }
        public string? PhoneNumber { get; set; }
        public string? About { get; set; }
        public string? Location { get; set; }
        public string? Company { get; set; }
        public string? JobTitle { get; set; }
        public string? LinkedinUrl { get; set; }
        public bool OpenToWork { get; set; } = false;
    }

    public class ExperienceDto
    {
        public int Id { get; set; }
        public string? PositionTitle { get; set; }
        public string? Company { get; set; }
        public string? FromDate { get; set; }
        public string? ToDate { get; set; }
        public string? Duration { get; set; }
        public string? Location { get; set; }
        public string? Description { get; set; }
        public string? LinkedinUrl { get; set; }
    }

    public class EducationDto
    {
        public int Id { get; set; }
        public string? Institution { get; set; }
        public string? Degree { get; set; }
        public string? FromDate { get; set; }
        public string? ToDate { get; set; }
        public string? Description { get; set; }
        public string? LinkedinUrl { get; set; }
    }

    public class InterestDto
    {
        public int Id { get; set; }
        public string? Title { get; set; }
    }

    public class AccomplishmentDto
    {
        public int Id { get; set; }
        public string? Category { get; set; }
        public string? Title { get; set; }
    }

    public class ContactDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Occupation { get; set; }
        public string? Url { get; set; }
    }

    public class SkillDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Proficiency { get; set; }
    }

}
