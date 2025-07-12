using findjobnuAPI.Models;

namespace findjobnuAPI.Services
{
    public interface IUserProfileService
    {
        Task<UserProfile?> GetByIdAsync(string id);
        Task<UserProfile?> CreateAsync(UserProfile userProfile);
        Task<bool> UpdateAsync(string id, UserProfile userProfile, string authenticatedUserId);
    }
}
