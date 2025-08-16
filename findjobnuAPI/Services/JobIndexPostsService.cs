using findjobnuAPI.Models;
using findjobnuAPI.Repositories.Context;
using Humanizer.Localisation.DateToOrdinalWords;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Logging;

namespace findjobnuAPI.Services
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
            _logger.LogInformation("Getting all job index posts. Page: {Page}, PageSize: {PageSize}", page, pageSize);
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
            _logger.LogInformation("Searching job index posts. SearchTerm: {SearchTerm}, Location: {Location}, Category: {Category}, PostedAfter: {PostedAfter}, PostedBefore: {PostedBefore}, Page: {Page}", searchTerm, location, category, postedAfter, postedBefore, page);
            var pageSize = 20;
            var query = _db.JobIndexPosts.Include(j => j.Categories).AsQueryable();

            static List<string> SplitWords(string? input) =>
                string.IsNullOrWhiteSpace(input)
                    ? []
                    : [.. input.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(w => w.Trim().ToLowerInvariant())];

            var searchWords = SplitWords(searchTerm);
            var locationWords = SplitWords(location);
            var categoryWords = SplitWords(category);

            if (locationWords.Count > 0)
            {
                foreach (var word in locationWords)
                {
                    var w = word;
                    query = query.Where(j =>
                        j.JobLocation != null &&
                        EF.Functions.Like(j.JobLocation!, "%" + w + "%")
                    );
                }
            }

            if (categoryWords.Count > 0)
            {
                foreach (var word in categoryWords)
                {
                    var w = word;
                    query = query.Where(j =>
                        j.Categories.Any(c => EF.Functions.Like(c.Name, "%" + w + "%"))
                    );
                }
            }

            if (searchWords.Count > 0)
            {
                foreach (var word in searchWords)
                {
                    var w = word;
                    query = query.Where(j =>
                        (j.JobTitle != null && EF.Functions.Like(j.JobTitle!, "%" + w + "%")) ||
                        (j.JobLocation != null && EF.Functions.Like(j.JobLocation!, "%" + w + "%")) ||
                        j.Categories.Any(c => EF.Functions.Like(c.Name, "%" + w + "%")) ||
                        (j.JobDescription != null && EF.Functions.Like(j.JobDescription!, "%" + w + "%"))
                    );
                }
            }

            if (postedAfter.HasValue)
                query = query.Where(j => j.Published >= postedAfter.Value);
            if (postedBefore.HasValue)
                query = query.Where(j => j.Published <= postedBefore.Value);

            var totalCount = await query.CountAsync();
            if (totalCount == 0) {
                _logger.LogInformation("No job index posts found for search criteria.");
                return new PagedList<JobIndexPosts>(0, pageSize, page, []);
            }

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
            _logger.LogInformation("Getting job index post by ID: {Id}", id);
            var result = await _db.JobIndexPosts
                .Include(j => j.Categories)
                .AsNoTracking()
                .FirstOrDefaultAsync(j => j.JobID == id);
            if (result == null)
                _logger.LogWarning("Job index post not found for ID: {Id}", id);
            return result ?? new JobIndexPosts();
        }

        public async Task<CategoriesResponse> GetCategoriesAsync()
        {
            try
            {
                _logger.LogInformation("Getting job categories and job counts.");
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
                _logger.LogError(ex, "Error getting job categories.");
                return new CategoriesResponse(false, ex.Message, []);
            }
        }

        public async Task<PagedList<JobIndexPosts>> GetSavedJobsByUserId(string userId, int page)
        {
            _logger.LogInformation("Getting saved jobs for user: {UserId}, Page: {Page}", userId, page);
            var profile = await _db.Profiles.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == userId);
            if (profile == null || profile.SavedJobPosts == null || !profile.SavedJobPosts.Any()) {
                _logger.LogWarning("No saved jobs found for user: {UserId}", userId);
                return new PagedList<JobIndexPosts>(0, 10, page, []);
            }

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
            _logger.LogInformation("Getting recommended jobs for user: {UserId}, Page: {Page}", userId, page);
            int pagesize = 20;

            var profileQuery = _db.Profiles
                .Include(p => p.BasicInfo)
                .Include(p => p.Experiences)
                .Include(p => p.Educations)
                .Include(p => p.Interests)
                .Include(p => p.Accomplishments)
                .Include(p => p.Contacts)
                .Include(p => p.Skills)
                .AsSplitQuery();

            var profile = await profileQuery.FirstOrDefaultAsync(x => x.UserId == userId);
            if (profile == null) {
                _logger.LogWarning("Profile not found for user: {UserId}", userId);
                return new PagedList<JobIndexPosts>(0, pagesize, page, []);
            }

            if ((profile.Keywords == null || profile.Keywords.Count == 0)
                && (profile.Experiences == null || profile.Experiences.Count == 0)
                && (profile.Interests == null || profile.Interests.Count == 0)
                && (profile.BasicInfo == null || profile.BasicInfo.JobTitle.IsNullOrEmpty())) {
                _logger.LogWarning("No keywords, experiences, interests, or job title found for user: {UserId}", userId);
                return new PagedList<JobIndexPosts>(0, pagesize, page, []);
            }

            var terms = GetKeywordsFromProfile(profile)
                .Where(k => !string.IsNullOrWhiteSpace(k))
                .Select(k => k.Trim())
                .Distinct()
                .Take(15)
                .ToList();

            if (terms.Count == 0)
            {
                _logger.LogWarning("No usable keywords derived for user: {UserId}", userId);
                return new PagedList<JobIndexPosts>(0, pagesize, page, []);
            }

            // Prefer exact keyword matches via JobKeywords table
            var matchingJobIds = _db.JobKeywords
                .Where(k => terms.Contains(k.Keyword))
                .Select(k => k.JobID)
                .Distinct();

            var totalCount = await matchingJobIds.CountAsync();
            if (totalCount == 0) {
                _logger.LogInformation("No recommended jobs found for user: {UserId}", userId);
                return new PagedList<JobIndexPosts>(0, pagesize, page, []);
            }

            var pageIds = await matchingJobIds
                .OrderBy(id => id)
                .Skip((page - 1) * pagesize)
                .Take(pagesize)
                .ToListAsync();

            var items = await _db.JobIndexPosts
                .Include(j => j.Categories)
                .Where(j => pageIds.Contains(j.JobID))
                .AsNoTracking()
                .OrderBy(j => j.JobID)
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