using findjobnuAPI.Models;
using findjobnuAPI.Models.LinkedIn;

namespace findjobnuAPI.Services
{
    public interface ILinkedInService
    {
        /// <summary>
        /// Get the LinkedIn OAuth authorization URL
        /// </summary>
        string GetAuthorizationUrl(string redirectUri, string state);

        /// <summary>
        /// Exchange authorization code for access token
        /// </summary>
        Task<LinkedInAuthResponse?> ExchangeCodeForTokenAsync(string authorizationCode, string redirectUri);

        /// <summary>
        /// Connect a user's LinkedIn account and store the profile
        /// </summary>
        Task<bool> ConnectLinkedInAccountAsync(string userId, string authorizationCode, string redirectUri);

        /// <summary>
        /// Sync LinkedIn profile data for a user
        /// </summary>
        Task<bool> SyncLinkedInProfileAsync(string userId, bool forceRefresh = false);

        /// <summary>
        /// Get LinkedIn profile for a user
        /// </summary>
        Task<LinkedInProfile?> GetLinkedInProfileAsync(string userId);

        /// <summary>
        /// Disconnect LinkedIn account for a user
        /// </summary>
        Task<bool> DisconnectLinkedInAccountAsync(string userId);

        /// <summary>
        /// Refresh LinkedIn access token
        /// </summary>
        Task<bool> RefreshLinkedInTokenAsync(string userId);

        /// <summary>
        /// Get user's work experience from LinkedIn
        /// </summary>
        Task<List<Models.LinkedInExperience>?> GetWorkExperienceAsync(string userId);

        /// <summary>
        /// Get user's education from LinkedIn
        /// </summary>
        Task<List<Models.LinkedInEducation>?> GetEducationAsync(string userId);

        /// <summary>
        /// Get user's skills from LinkedIn
        /// </summary>
        Task<List<string>?> GetSkillsAsync(string userId);
    }
}