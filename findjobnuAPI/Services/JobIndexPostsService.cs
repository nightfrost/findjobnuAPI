using Humanizer.Localisation.DateToOrdinalWords;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Logging;
using FindjobnuService.Repositories.Context;
using FindjobnuService.Models;

namespace FindjobnuService.Services
{
    public class JobIndexPostsService : IJobIndexPostsService
    {
        private readonly FindjobnuContext _db;
        private readonly ILogger<JobIndexPostsService> _logger;

        public JobIndexPostsService(FindjobnuContext db, ILogger<JobIndexPostsService> logger)
        {
            _db = db;
            _logger = logger;
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

        public async Task<PagedList<JobIndexPosts>> SearchAsync(string? searchTerm, string? location, string? category, DateTime? postedAfter, DateTime? postedBefore, int page)
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
            if (totalCount == 0) return new PagedList<JobIndexPosts>(0, pageSize, page, []);

            var items = await query
                .OrderBy(j => j.JobID)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();

            return new PagedList<JobIndexPosts>(totalCount, pageSize, page, items);
        }

        public async Task<JobIndexPosts> GetByIdAsync(int id)
        {
            return await _db.JobIndexPosts
                .Include(j => j.Categories)
                .AsNoTracking()
                .FirstOrDefaultAsync(j => j.JobID == id) ?? new JobIndexPosts();
        }

        public async Task<CategoriesResponse> GetCategoriesAsync()
        {
            try
            {
                var categoryJobCounts = await _db.Categories
                    .Select(c => new
                    {
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
                _logger.LogError(ex, "Failed to get categories");
                return new CategoriesResponse(false, ex.Message, []);
            }
        }

        public async Task<PagedList<JobIndexPosts>> GetSavedJobsByUserId(string userId, int page)
        {
            var profile = await _db.Profiles.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == userId);
            if (profile == null || profile.SavedJobPosts == null || !profile.SavedJobPosts.Any())
                return new PagedList<JobIndexPosts>(0, 10, page, []);

            var jobIds = profile.SavedJobPosts
                .Select(id => int.TryParse(id, out var jid) ? jid : (int?)null)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .ToList();

            var jobs = await _db.JobIndexPosts.Where(j => jobIds.Contains(j.JobID)).ToListAsync();
            return new PagedList<JobIndexPosts>(jobs.Count, 10, page, jobs);
        }

        public async Task<PagedList<JobIndexPosts>> GetRecommendedJobsByUserAndProfile(string userId, int page)
        {
            int pagesize = 20;

            var profile = await _db.Profiles
                .Include(p => p.BasicInfo)
                .Include(p => p.Experiences)
                .Include(p => p.Educations)
                .Include(p => p.Interests)
                .Include(p => p.Accomplishments)
                .Include(p => p.Contacts)
                .Include(p => p.Skills)
                .FirstOrDefaultAsync(x => x.UserId == userId);
            if (profile == null)
                return new PagedList<JobIndexPosts>(0, pagesize, page, []);

            if ((profile.Keywords == null || profile.Keywords.Count == 0)
                && (profile.Experiences == null || profile.Experiences.Count == 0)
                && (profile.Interests == null || profile.Interests.Count == 0)
                && (profile.BasicInfo == null || profile.BasicInfo.JobTitle.IsNullOrEmpty()))
                    return new PagedList<JobIndexPosts>(0, pagesize, page, []);

            var keywords = GetKeywordsFromProfile(profile);

            var baseQuery = _db.JobIndexPosts
                .Include(j => j.Categories)
                .Where(j => (j.Keywords != null && j.Keywords.Any(k => keywords.Contains(k)))
                || (j.CompanyName != null && keywords.Contains(j.CompanyName))
                || (j.JobTitle != null && keywords.Contains(j.JobTitle)))
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

        private static HashSet<string> GetKeywordsFromProfile(Profile profile)
        {
            var keywords = new HashSet<string>();
            if (profile.Keywords != null)
            {
                foreach (var keyword in profile.Keywords)
                {
                    if (!string.IsNullOrWhiteSpace(keyword))
                        keywords.Add(keyword);
                }
            }
            if (profile.BasicInfo != null)
            {
                if (!string.IsNullOrWhiteSpace(profile.BasicInfo.JobTitle))
                    keywords.Add(profile.BasicInfo.JobTitle);
                if (!string.IsNullOrWhiteSpace(profile.BasicInfo.Company))
                    keywords.Add(profile.BasicInfo.Company);
            }
            if (profile.Experiences != null)
            {
                foreach (var exp in profile.Experiences)
                {
                    if (!string.IsNullOrWhiteSpace(exp.PositionTitle))
                        keywords.Add(exp.PositionTitle);
                    if (!string.IsNullOrWhiteSpace(exp.Company))
                        keywords.Add(exp.Company);
                }
            }
            if (profile.Interests != null)
            {
                foreach (var interest in profile.Interests)
                {
                    if (!string.IsNullOrWhiteSpace(interest.Title))
                        keywords.Add(interest.Title);
                }
            }
            if (profile.Skills != null)
            {
                foreach (var skill in profile.Skills)
                {
                    if (!string.IsNullOrWhiteSpace(skill.Name))
                        keywords.Add(skill.Name);
                }
            }
            return keywords;
        }
    }
}