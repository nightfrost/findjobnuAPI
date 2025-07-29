using findjobnuAPI.Models;

namespace findjobnuAPI.Services
{
    public interface IUserProfileService
    {
        Task<UserProfile?> GetByUserIdAsync(string id);
        Task<UserProfile?> CreateAsync(UserProfile userProfile);
        Task<bool> UpdateAsync(int id, UserProfile userProfile, string authenticatedUserId);
        Task<List<string>> GetSavedJobsByUserIdAsync(string userId);
        Task<bool> SaveJobAsync(string userId, string jobId);
        Task<bool> RemoveSavedJobAsync(string userId, string jobId);
        // No new methods needed for Keywords, as it's part of UserProfile
    }
}
