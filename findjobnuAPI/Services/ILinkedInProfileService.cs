using findjobnuAPI.Models;
using System.Threading.Tasks;

namespace findjobnuAPI.Services
{
    public interface ILinkedInProfileService
    {
        Task<LinkedInProfileResult> GetProfileAsync(string userId);
        Task<bool> SaveProfileAsync(string userId, Profile profile); // Use Profile
    }
}
