using FindjobnuService.DTOs.Requests;
using FindjobnuService.Models;

namespace FindjobnuService.Mappers
{
    public static class ProfileUpdateMapper
    {
        public static Profile ToModel(ProfileUpdateRequest request)
        {
            return new Profile
            {
                UserId = request.UserId,
                BasicInfo = new BasicInfo
                {
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    DateOfBirth = request.DateOfBirth,
                    PhoneNumber = request.PhoneNumber,
                    About = request.About,
                    Location = request.Location,
                    Company = request.Company,
                    JobTitle = request.JobTitle,
                    LinkedinUrl = request.LinkedinUrl,
                    OpenToWork = request.OpenToWork
                },
                Keywords = request.Keywords,
                SavedJobPosts = request.SavedJobPosts,
                Experiences = request.Experiences?.Select(e => new Experience
                {
                    PositionTitle = e.PositionTitle,
                    Company = e.Company,
                    FromDate = e.FromDate,
                    ToDate = e.ToDate,
                    Duration = e.Duration,
                    Location = e.Location,
                    Description = e.Description,
                    LinkedinUrl = e.LinkedinUrl
                }).ToList(),
                Educations = request.Educations?.Select(e => new Education
                {
                    Institution = e.Institution,
                    Degree = e.Degree,
                    FromDate = e.FromDate,
                    ToDate = e.ToDate,
                    Description = e.Description,
                    LinkedinUrl = e.LinkedinUrl
                }).ToList(),
                Interests = request.Interests?.Select(i => new Interest
                {
                    Title = i.Title ?? string.Empty
                }).ToList(),
                Accomplishments = request.Accomplishments?.Select(a => new Accomplishment
                {
                    Category = a.Category,
                    Title = a.Title
                }).ToList(),
                Contacts = request.Contacts?.Select(c => new Contact
                {
                    Name = c.Name,
                    Occupation = c.Occupation,
                    Url = c.Url
                }).ToList(),
                Skills = request.Skills?.Select(s => new Skill
                {
                    Name = s.Name,
                    Proficiency = s.Proficiency switch
                    {
                        SkillProficiencyUpdate.Beginner => SkillProficiency.Beginner,
                        SkillProficiencyUpdate.Intermediate => SkillProficiency.Intermediate,
                        SkillProficiencyUpdate.Advanced => SkillProficiency.Advanced,
                        SkillProficiencyUpdate.Expert => SkillProficiency.Expert,
                        _ => SkillProficiency.Beginner
                    }
                }).ToList()
            };
        }
    }
}
