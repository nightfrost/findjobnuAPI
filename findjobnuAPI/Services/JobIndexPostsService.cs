using Microsoft.EntityFrameworkCore;
using findjobnuAPI.Models;
using findjobnuAPI.Repositories.Context;
using Humanizer.Localisation.DateToOrdinalWords;
using Microsoft.IdentityModel.Tokens;

namespace findjobnuAPI.Services
{
    public class JobIndexPostsService : IJobIndexPostsService
    {
        private readonly FindjobnuContext _db;

        public JobIndexPostsService(FindjobnuContext db)
        {
            _db = db;
        }

        public async Task<PagedList<JobIndexPosts>> GetAllAsync(int page, int pageSize)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;

            var totalCount = await _db.JobIndexPosts.CountAsync();
            var items = await _db.JobIndexPosts
                .Include(j => j.Categories)
                .OrderBy(j => j.JobID)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();

            return new PagedList<JobIndexPosts>(totalCount, pageSize, page, items);
        }

        public async Task<PagedList<JobIndexPosts>?> SearchAsync(string? searchTerm, string? location, string? category, DateTime? postedAfter, DateTime? postedBefore, int page)
        {
            var pageSize = 20;
            var query = _db.JobIndexPosts.Include(j => j.Categories).AsQueryable();

            if (!string.IsNullOrEmpty(location))
                query = query.Where(j => j.JobLocation != null && j.JobLocation.Contains(location));
            if (!string.IsNullOrEmpty(category))
                query = query.Where(j => j.Categories.Any(c => c.Name.Contains(category)));
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(j =>
                    (j.JobTitle != null && j.JobTitle.Contains(searchTerm)) ||
                    (j.JobLocation != null && j.JobLocation.Contains(searchTerm)) ||
                    (j.Categories.Any(c => c.Name.Contains(searchTerm))) ||
                    (j.JobDescription != null && j.JobDescription.Contains(searchTerm))
                );
            }
            if (postedAfter.HasValue)
                query = query.Where(j => j.Published >= postedAfter.Value);
            if (postedBefore.HasValue)
                query = query.Where(j => j.Published <= postedBefore.Value);

            var totalCount = await query.CountAsync();
            if (totalCount == 0) return null;

            var items = await query
                .OrderBy(j => j.JobID)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();

            return new PagedList<JobIndexPosts>(totalCount, pageSize, page, items);
        }

        public async Task<JobIndexPosts?> GetByIdAsync(int id)
        {
            return await _db.JobIndexPosts.AsNoTracking()
                .Include(j => j.Categories)
                .FirstOrDefaultAsync(model => model.JobID == id);
        }

        public async Task<CategoriesResponse> GetCategoriesAsync()
        {
            try
            {
                var categoryJobCounts = await _db.Categories
                    .Select(c => new {
                        c.Name,
                        NumberOfJobs = c.JobIndexPosts.Count()
                    })
                    .OrderBy(x => x.Name)
                    .ToListAsync();

                var dict = categoryJobCounts.ToDictionary(x => x.Name, x => x.NumberOfJobs);
                return new CategoriesResponse(true, null, dict);
            }
            catch (Exception ex)
            {
                return new CategoriesResponse(false, ex.Message, []);
            }
        }

        public async Task<PagedList<JobIndexPosts>> GetSavedJobsByUserId(string userid, int page)
        {
            int pagesize = 20;

            var userSavedJobIds = await _db.UserProfile
                .Where(up => up.UserId == userid)
                .Select(up => up.SavedJobPosts)
                .FirstOrDefaultAsync();

            if (userSavedJobIds == null || userSavedJobIds.Count == 0)
                return new PagedList<JobIndexPosts>(0, pagesize, page, []);

            var jobIds = userSavedJobIds
                .Select(id => int.TryParse(id, out var intId) ? (int?)intId : null)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .ToList();

            var query = _db.JobIndexPosts
                .Include(j => j.Categories)
                .Where(j => jobIds.Contains(j.JobID))
                .Skip((page - 1) * pagesize)
                .Take(pagesize)
                .AsNoTracking();

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderBy(j => j.JobID)
                .ToListAsync();

            return new PagedList<JobIndexPosts>(totalCount, pagesize, page, items);
        }

        public async Task<PagedList<JobIndexPosts>> GetRecommendedJobsByUserAndWorkProfile(UserProfile userProfile, WorkProfile workProfile, int page)
        {
            int pagesize = 20;

            //verify that userProfile and workProfile have atleast 1 reference to relevant data
            if ((userProfile.Keywords == null || userProfile.Keywords.Count == 0)
                && (workProfile.Experiences == null || workProfile.Experiences.Count == 0)
                && (workProfile.Interests == null || workProfile.Interests.Count == 0)
                && (workProfile.BasicInfo == null || workProfile.BasicInfo.JobTitle.IsNullOrEmpty()))
                return new PagedList<JobIndexPosts>(0, pagesize, page, []);

            var upKeywords = userProfile.Keywords?.Select(k => k.Trim()).ToHashSet();
            var wpKeywords = GetKeywordsFromWorkProfile(workProfile);
            
            if (upKeywords != null && upKeywords.Count > 0)
                wpKeywords.UnionWith(upKeywords);

            var baseQuery = _db.JobIndexPosts
                .Include(j => j.Categories)
                .Where(j => (j.Keywords != null && j.Keywords.Any(k => wpKeywords.Contains(k))) 
                || (j.CompanyName != null && wpKeywords.Contains(j.CompanyName))
                || (j.JobTitle != null && wpKeywords.Contains(j.JobTitle)))
                .AsNoTracking();

            var totalCount = await baseQuery.CountAsync();
            if (totalCount == 0)
                return new PagedList<JobIndexPosts>(0, pagesize, page, []);

            var items = await baseQuery
                .OrderBy(j => j.JobID)
                .Skip((page - 1) * pagesize)
                .Take(pagesize)
                .ToListAsync();
            return new PagedList<JobIndexPosts>(totalCount, pagesize, page, items);
        }

        private static HashSet<string> GetKeywordsFromWorkProfile(WorkProfile workProfile)
        {
            var keywords = new HashSet<string>();
            if (workProfile.BasicInfo != null)
            {
                if (!string.IsNullOrWhiteSpace(workProfile.BasicInfo.JobTitle))
                    keywords.Add(workProfile.BasicInfo.JobTitle);
                if (!string.IsNullOrWhiteSpace(workProfile.BasicInfo.Company))
                    keywords.Add(workProfile.BasicInfo.Company);
            }
            if (workProfile.Experiences != null)
            {
                foreach (var exp in workProfile.Experiences)
                {
                    if (!string.IsNullOrWhiteSpace(exp.PositionTitle))
                        keywords.Add(exp.PositionTitle);
                    if (!string.IsNullOrWhiteSpace(exp.Company))
                        keywords.Add(exp.Company);
                }
            }
            if (workProfile.Interests != null)
            {
                foreach (var interest in workProfile.Interests)
                {
                    if (!string.IsNullOrWhiteSpace(interest.Title))
                        keywords.Add(interest.Title);
                }
            }
            if (workProfile.Skills != null)
            {
                foreach (var skill in workProfile.Skills)
                {
                    if (!string.IsNullOrWhiteSpace(skill.Name))
                        keywords.Add(skill.Name);
                }
            }
            return keywords;
        }
    }
}