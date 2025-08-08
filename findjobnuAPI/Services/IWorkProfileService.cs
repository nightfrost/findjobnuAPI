using findjobnuAPI.Models;

namespace findjobnuAPI.Services
{
    public interface IWorkProfileService
    {
        Task<WorkProfile?> GetByUserProfileIdAsync(int userProfileId);
        Task<WorkProfile?> CreateAsync(WorkProfile workProfile);
        Task<bool> UpdateAsync(int id, WorkProfile workProfile, string authenticatedUserId);
        Task<bool> DeleteAsync(int id, string authenticatedUserId);
    }
}
