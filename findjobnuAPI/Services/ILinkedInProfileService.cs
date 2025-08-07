using findjobnuAPI.Models;

namespace findjobnuAPI.Services
{
    public interface ILinkedInProfileService
    {
        Task<LinkedInProfileResult> GetProfileAsync(string userId);
        Task<bool> SaveProfileAsync(string userId, WorkProfile profile); // Use WorkProfile
    }
}
