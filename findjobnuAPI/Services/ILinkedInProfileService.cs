using FindjobnuService.Models;
using System.Threading.Tasks;

namespace FindjobnuService.Services
{
    public interface ILinkedInProfileService
    {
        Task<LinkedInProfileResult> GetProfileAsync(string userId);
        Task<bool> SaveProfileAsync(string userId, Profile profile); // Use Profile
    }
}
