using findjobnuAPI.Repositories.Context;
using findjobnuAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace findjobnuAPI.Services
{
    public class UserProfileService : IUserProfileService
    {
        private readonly FindjobnuContext _db;

        public UserProfileService(FindjobnuContext db)
        {
            _db = db;
        }

        public async Task<UserProfile?> GetByIdAsync(int id)
        {
            return await _db.UserProfile.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<UserProfile?> CreateAsync(UserProfile userProfile)
        {
            _db.UserProfile.Add(userProfile);
            await _db.SaveChangesAsync();
            return userProfile;
        }

        public async Task<bool> UpdateAsync(int id, UserProfile userProfile, string authenticatedUserId)
        {
            var affected = await _db.UserProfile
                .Where(model => model.Id == id && model.UserId == authenticatedUserId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(m => m.UserId, userProfile.UserId)
                    .SetProperty(m => m.FirstName, userProfile.FirstName)
                    .SetProperty(m => m.LastName, userProfile.LastName)
                    .SetProperty(m => m.DateOfBirth, userProfile.DateOfBirth)
                    .SetProperty(m => m.PhoneNumber, userProfile.PhoneNumber)
                    .SetProperty(m => m.Address, userProfile.Address)
                    .SetProperty(m => m.LastUpdatedAt, userProfile.LastUpdatedAt)
                    .SetProperty(m => m.SavedJobPosts, userProfile.SavedJobPosts)
                );
            return affected == 1;
        }
    }
}
