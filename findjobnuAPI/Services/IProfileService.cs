using findjobnuAPI.Models;

namespace findjobnuAPI.Services
{
    public interface IProfileService
    {
        Task<Profile?> GetByUserIdAsync(string userId);
        Task<Profile?> CreateAsync(Profile profile);
        Task<bool> UpdateAsync(int id, Profile profile, string authenticatedUserId);
        Task<List<string>> GetSavedJobsByUserIdAsync(string userId);
        Task<bool> SaveJobAsync(string userId, string jobId);
        Task<bool> RemoveSavedJobAsync(string userId, string jobId);
    }
}
