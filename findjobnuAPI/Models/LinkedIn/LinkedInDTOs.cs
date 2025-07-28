namespace findjobnuAPI.Models.LinkedIn
{
    // DTOs for LinkedIn API responses
    public class LinkedInAuthResponse
    {
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
        public int ExpiresIn { get; set; }
        public string? TokenType { get; set; }
        public string? Scope { get; set; }
    }

    public class LinkedInUserProfileResponse
    {
        public string? Id { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Headline { get; set; }
        public string? Summary { get; set; }
        public string? Industry { get; set; }
        public LinkedInLocation? Location { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public string? PublicProfileUrl { get; set; }
    }

    public class LinkedInLocation
    {
        public string? Name { get; set; }
        public string? Country { get; set; }
    }

    public class LinkedInPositionsResponse
    {
        public LinkedInPosition[]? Values { get; set; }
        public int Total { get; set; }
    }

    public class LinkedInPosition
    {
        public string? Title { get; set; }
        public string? Summary { get; set; }
        public LinkedInCompany? Company { get; set; }
        public LinkedInDate? StartDate { get; set; }
        public LinkedInDate? EndDate { get; set; }
        public bool IsCurrent { get; set; }
        public LinkedInLocation? Location { get; set; }
    }

    public class LinkedInCompany
    {
        public string? Name { get; set; }
        public string? Industry { get; set; }
        public int Size { get; set; }
    }

    public class LinkedInEducationsResponse
    {
        public LinkedInEducation[]? Values { get; set; }
        public int Total { get; set; }
    }

    public class LinkedInEducation
    {
        public string? SchoolName { get; set; }
        public string? FieldOfStudy { get; set; }
        public string? Degree { get; set; }
        public LinkedInDate? StartDate { get; set; }
        public LinkedInDate? EndDate { get; set; }
        public string? Notes { get; set; }
    }

    public class LinkedInSkillsResponse
    {
        public LinkedInSkill[]? Values { get; set; }
        public int Total { get; set; }
    }

    public class LinkedInSkill
    {
        public string? Name { get; set; }
        public LinkedInSkillProficiency? Proficiency { get; set; }
    }

    public class LinkedInSkillProficiency
    {
        public string? Level { get; set; }
    }

    public class LinkedInDate
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int Day { get; set; }
    }

    // Request DTOs
    public class LinkedInConnectRequest
    {
        public string? AuthorizationCode { get; set; }
        public string? RedirectUri { get; set; }
    }

    public class LinkedInSyncRequest
    {
        public string? UserId { get; set; }
        public bool ForceRefresh { get; set; } = false;
    }
}