using findjobnuAPI.Models;
using findjobnuAPI.Repositories.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace findjobnuAPI.Services
{
    public class ProfileService : IProfileService
    {
        private readonly FindjobnuContext _db;

        public ProfileService(FindjobnuContext db)
        {
            _db = db;
        }

        public async Task<Profile?> GetByUserIdAsync(string userId)
        {
            return await _db.Profiles
                .Include(p => p.BasicInfo)
                .Include(p => p.Experiences)
                .Include(p => p.Educations)
                .Include(p => p.Interests)
                .Include(p => p.Accomplishments)
                .Include(p => p.Contacts)
                .Include(p => p.Skills)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.UserId == userId);
        }

        public async Task<Profile?> CreateAsync(Profile profile)
        {
            _db.Profiles.Add(profile);
            await _db.SaveChangesAsync();
            return profile;
        }

        public async Task<bool> UpdateAsync(int id, Profile profile, string authenticatedUserId)
        {
            var entity = await _db.Profiles
                .Include(p => p.BasicInfo)
                .Include(p => p.Experiences)
                .Include(p => p.Educations)
                .Include(p => p.Interests)
                .Include(p => p.Accomplishments)
                .Include(p => p.Contacts)
                .Include(p => p.Skills)
                .FirstOrDefaultAsync(model => model.Id == id && model.UserId == authenticatedUserId);
            if (entity == null)
                return false;

            // Update fields now in BasicInfo
            entity.BasicInfo.FirstName = profile.BasicInfo.FirstName;
            entity.BasicInfo.LastName = profile.BasicInfo.LastName;
            entity.BasicInfo.DateOfBirth = profile.BasicInfo.DateOfBirth;
            entity.BasicInfo.PhoneNumber = profile.BasicInfo.PhoneNumber;
            entity.BasicInfo.About = profile.BasicInfo.About;
            entity.BasicInfo.Location = profile.BasicInfo.Location;
            entity.BasicInfo.Company = profile.BasicInfo.Company;
            entity.BasicInfo.JobTitle = profile.BasicInfo.JobTitle;
            entity.BasicInfo.LinkedinUrl = profile.BasicInfo.LinkedinUrl;
            entity.BasicInfo.OpenToWork = profile.BasicInfo.OpenToWork;
            entity.LastUpdatedAt = DateTime.UtcNow;
            entity.Keywords = profile.Keywords;
            entity.SavedJobPosts = profile.SavedJobPosts;

            // Replace all related collections
            _db.Experiences.RemoveRange(entity.Experiences ?? []);
            if (profile.Experiences != null)
            {
                foreach (var exp in profile.Experiences)
                {
                    exp.ProfileId = entity.Id;
                    _db.Experiences.Add(exp);
                }
            }
            _db.Educations.RemoveRange(entity.Educations ?? []);
            if (profile.Educations != null)
            {
                foreach (var edu in profile.Educations)
                {
                    edu.ProfileId = entity.Id;
                    _db.Educations.Add(edu);
                }
            }
            _db.Interests.RemoveRange(entity.Interests ?? []);
            if (profile.Interests != null)
            {
                foreach (var interest in profile.Interests)
                {
                    interest.ProfileId = entity.Id;
                    _db.Interests.Add(interest);
                }
            }
            _db.Accomplishments.RemoveRange(entity.Accomplishments ?? []);
            if (profile.Accomplishments != null)
            {
                foreach (var acc in profile.Accomplishments)
                {
                    acc.ProfileId = entity.Id;
                    _db.Accomplishments.Add(acc);
                }
            }
            _db.Contacts.RemoveRange(entity.Contacts ?? []);
            if (profile.Contacts != null)
            {
                foreach (var contact in profile.Contacts)
                {
                    contact.ProfileId = entity.Id;
                    _db.Contacts.Add(contact);
                }
            }
            _db.Skills.RemoveRange(entity.Skills ?? []);
            if (profile.Skills != null)
            {
                foreach (var skill in profile.Skills)
                {
                    skill.ProfileId = entity.Id;
                    _db.Skills.Add(skill);
                }
            }

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<List<string>> GetSavedJobsByUserIdAsync(string userId)
        {
            var profile = await _db.Profiles
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.UserId == userId);
            if (profile == null)
            {
                return new List<string>();
            }
            return profile.SavedJobPosts ?? new List<string>();
        }

        public async Task<bool> SaveJobAsync(string userId, string jobId)
        {
            var profile = await _db.Profiles
                .FirstOrDefaultAsync(x => x.UserId == userId);
            if (profile == null)
            {
                return false;
            }
            if (profile.SavedJobPosts == null)
            {
                profile.SavedJobPosts = new List<string>();
            }
            if (!profile.SavedJobPosts.Contains(jobId))
            {
                profile.SavedJobPosts.Add(jobId);
                await _db.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public async Task<bool> RemoveSavedJobAsync(string userId, string jobId)
        {
            var profile = await _db.Profiles
                .FirstOrDefaultAsync(x => x.UserId == userId);

            if (profile == null || profile.SavedJobPosts == null) return false;
            
            if (profile.SavedJobPosts.Remove(jobId))
            {
                await _db.SaveChangesAsync();
                return true;
            }
            return false;
        }
    }
}
