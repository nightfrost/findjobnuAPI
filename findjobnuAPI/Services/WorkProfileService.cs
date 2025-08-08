using findjobnuAPI.Models;
using findjobnuAPI.Repositories.Context;
using Microsoft.EntityFrameworkCore;

namespace findjobnuAPI.Services
{
    public class WorkProfileService : IWorkProfileService
    {
        private readonly FindjobnuContext _db;
        public WorkProfileService(FindjobnuContext db)
        {
            _db = db;
        }

        public async Task<WorkProfile?> GetByUserProfileIdAsync(int userProfileId)
        {
            return await _db.WorkProfiles
                .Include(wp => wp.BasicInfo)
                .Include(wp => wp.Experiences)
                .Include(wp => wp.Educations)
                .Include(wp => wp.Interests)
                .Include(wp => wp.Accomplishments)
                .Include(wp => wp.Contacts)
                .Include(wp => wp.Skills)
                .FirstOrDefaultAsync(wp => wp.UserProfileId == userProfileId);
        }

        public async Task<WorkProfile?> CreateAsync(WorkProfile workProfile)
        {
            _db.WorkProfiles.Add(workProfile);
            await _db.SaveChangesAsync();
            return workProfile;
        }

        public async Task<bool> UpdateAsync(int id, WorkProfile workProfile, string authenticatedUserId)
        {
            var entity = await _db.WorkProfiles
                .Include(wp => wp.UserProfile)
                .Include(wp => wp.Experiences)
                .Include(wp => wp.Educations)
                .Include(wp => wp.Interests)
                .Include(wp => wp.Accomplishments)
                .Include(wp => wp.Contacts)
                .Include(wp => wp.Skills)
                .FirstOrDefaultAsync(wp => wp.Id == id && wp.UserProfile.UserId == authenticatedUserId);
            if (entity == null)
                return false;

            // Update BasicInfo
            entity.BasicInfo = workProfile.BasicInfo;

            // Update Experiences
            _db.Experiences.RemoveRange(entity.Experiences ?? []);
            if (workProfile.Experiences != null)
            {
                foreach (var exp in workProfile.Experiences)
                {
                    exp.WorkProfileId = entity.Id;
                    _db.Experiences.Add(exp);
                }
            }

            // Update Educations
            _db.Educations.RemoveRange(entity.Educations ?? []);
            if (workProfile.Educations != null)
            {
                foreach (var edu in workProfile.Educations)
                {
                    edu.WorkProfileId = entity.Id;
                    _db.Educations.Add(edu);
                }
            }

            // Update Interests
            _db.Interests.RemoveRange(entity.Interests ?? []);
            if (workProfile.Interests != null)
            {
                foreach (var interest in workProfile.Interests)
                {
                    interest.WorkProfileId = entity.Id;
                    _db.Interests.Add(interest);
                }
            }

            // Update Accomplishments
            _db.Accomplishments.RemoveRange(entity.Accomplishments ?? []);
            if (workProfile.Accomplishments != null)
            {
                foreach (var acc in workProfile.Accomplishments)
                {
                    acc.WorkProfileId = entity.Id;
                    _db.Accomplishments.Add(acc);
                }
            }

            // Update Contacts
            _db.Contacts.RemoveRange(entity.Contacts ?? []);
            if (workProfile.Contacts != null)
            {
                foreach (var contact in workProfile.Contacts)
                {
                    contact.WorkProfileId = entity.Id;
                    _db.Contacts.Add(contact);
                }
            }

            // Update Skills
            _db.Skills.RemoveRange(entity.Skills ?? []);
            if (workProfile.Skills != null)
            {
                foreach (var skill in workProfile.Skills)
                {
                    skill.WorkProfileId = entity.Id;
                    _db.Skills.Add(skill);
                }
            }

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id, string authenticatedUserId)
        {
            var entity = await _db.WorkProfiles
                .Include(wp => wp.UserProfile)
                .FirstOrDefaultAsync(wp => wp.Id == id && wp.UserProfile.UserId == authenticatedUserId);
            if (entity == null)
                return false;
            _db.WorkProfiles.Remove(entity);
            await _db.SaveChangesAsync();
            return true;
        }
    }
}
