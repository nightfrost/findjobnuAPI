using findjobnuAPI.Models;

namespace findjobnuAPI.Services
{
    public interface IUserProfileService
    {
        Task<UserProfile?> GetByIdAsync(int id);
        Task<UserProfile?> CreateAsync(UserProfile userProfile);
        Task<bool> UpdateAsync(int id, UserProfile userProfile, string authenticatedUserId);
        Task<List<string>> GetSavedJobsByUserIdAsync(int userId);
        Task<bool> SaveJobAsync(int userId, string jobId);
    }
}
