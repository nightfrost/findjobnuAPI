namespace FindjobnuService.DTOs.Requests;

using System.ComponentModel.DataAnnotations;

public record ProfileUpdateRequest(
    [property: Required] string UserId,
    // Basic info
    [property: Required, MaxLength(50)] string FirstName,
    [property: Required, MaxLength(100)] string LastName,
    DateTime? DateOfBirth,
    [property: Phone, MaxLength(100)] string? PhoneNumber,
    string? About,
    string? Location,
    string? Company,
    string? JobTitle,
    string? LinkedinUrl,
    bool OpenToWork,
    // Collections
    List<ExperienceUpdate>? Experiences,
    List<EducationUpdate>? Educations,
    List<InterestUpdate>? Interests,
    List<AccomplishmentUpdate>? Accomplishments,
    List<ContactUpdate>? Contacts,
    List<SkillUpdate>? Skills,
    // Other profile fields
    List<string>? Keywords,
    List<string>? SavedJobPosts
);

public record ExperienceUpdate(
    string? PositionTitle,
    string? Company,
    string? FromDate,
    string? ToDate,
    string? Duration,
    string? Location,
    string? Description,
    string? LinkedinUrl
);

public record EducationUpdate(
    string? Institution,
    string? Degree,
    string? FromDate,
    string? ToDate,
    string? Description,
    string? LinkedinUrl
);

public record InterestUpdate(
    string? Title
);

public record AccomplishmentUpdate(
    string? Category,
    string? Title
);

public record ContactUpdate(
    string? Name,
    string? Occupation,
    string? Url
);

public enum SkillProficiencyUpdate
{
    Beginner,
    Intermediate,
    Advanced,
    Expert
}

public record SkillUpdate(
    [property: Required] string Name,
    [property: Required] SkillProficiencyUpdate Proficiency
);
