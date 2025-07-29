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

        public async Task<UserProfile?> GetByUserIdAsync(string id)
        {
            return await _db.UserProfile.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == id);
        }

        public async Task<UserProfile?> CreateAsync(UserProfile userProfile)
        {
            _db.UserProfile.Add(userProfile);
            await _db.SaveChangesAsync();
            return userProfile;
        }

        public async Task<bool> UpdateAsync(int id, UserProfile userProfile, string authenticatedUserId)
        {
            var providerName = _db.Database.ProviderName;
            if (providerName != null && providerName.Contains("InMemory"))
            {
                // Fallback for InMemory provider (used in tests)
                var entity = await _db.UserProfile
                    .FirstOrDefaultAsync(model => model.Id == id && model.UserId == authenticatedUserId);
                if (entity == null)
                    return false;

                entity.UserId = userProfile.UserId;
                entity.FirstName = userProfile.FirstName;
                entity.LastName = userProfile.LastName;
                entity.DateOfBirth = userProfile.DateOfBirth;
                entity.PhoneNumber = userProfile.PhoneNumber;
                entity.Address = userProfile.Address;
                entity.City = userProfile.City;
                entity.LastUpdatedAt = DateTime.UtcNow;
                entity.Keywords = userProfile.Keywords;

                await _db.SaveChangesAsync();
                return true;
            }
            else
            {
                // Use bulk update for relational providers
                var affected = await _db.UserProfile
                    .Where(model => model.Id == id && model.UserId == authenticatedUserId)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(m => m.UserId, userProfile.UserId)
                        .SetProperty(m => m.FirstName, userProfile.FirstName)
                        .SetProperty(m => m.LastName, userProfile.LastName)
                        .SetProperty(m => m.DateOfBirth, userProfile.DateOfBirth)
                        .SetProperty(m => m.PhoneNumber, userProfile.PhoneNumber)
                        .SetProperty(m => m.Address, userProfile.Address)
                        .SetProperty(m => m.LastUpdatedAt, DateTime.UtcNow)
                        .SetProperty(m => m.SavedJobPosts, userProfile.SavedJobPosts)
                        .SetProperty(m => m.City, userProfile.City)
                        .SetProperty(m => m.Keywords, userProfile.Keywords)
                    );
                return affected == 1;
            }
        }

        public async Task<List<string>> GetSavedJobsByUserIdAsync(string userId)
        {
            var userProfile = await _db.UserProfile
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.UserId == userId);
            if (userProfile == null)
            {
                return new List<string>();
            }
            return userProfile.SavedJobPosts ?? [];
        }

        public async Task<bool> SaveJobAsync(string userId, string jobId)
        {
            var userProfile = await _db.UserProfile
                .FirstOrDefaultAsync(x => x.UserId == userId);
            if (userProfile == null)
            {
                return false;
            }
            if (userProfile.SavedJobPosts == null)
            {
                userProfile.SavedJobPosts = new List<string>();
            }
            if (!userProfile.SavedJobPosts.Contains(jobId))
            {
                userProfile.SavedJobPosts.Add(jobId);
                await _db.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public async Task<bool> RemoveSavedJobAsync(string userId, string jobId)
        {
            var userProfile = await _db.UserProfile
                .FirstOrDefaultAsync(x => x.UserId == userId);

            if (userProfile == null || userProfile.SavedJobPosts == null) return false;
            
            if (userProfile.SavedJobPosts.Remove(jobId))
            {
                await _db.SaveChangesAsync();
                return true;
            }
            return false;
        }
    }
}
