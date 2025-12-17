using FindjobnuService.DTOs.Responses;
using FindjobnuService.Models;
using FindjobnuService.Repositories.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace FindjobnuService.Services
{
    public class JobIndexPostsService : IJobIndexPostsService
    {
        private readonly FindjobnuContext _db;
        private readonly ILogger<JobIndexPostsService> _logger;
        private readonly IMemoryCache _cache;

        public JobIndexPostsService(FindjobnuContext db, ILogger<JobIndexPostsService> logger, IMemoryCache cache)
        {
            _db = db;
            _logger = logger;
            _cache = cache;
        }

        // Backward-compatible constructor used by worker/tests
        public JobIndexPostsService(FindjobnuContext db, ILogger<JobIndexPostsService> logger)
            : this(db, logger, new MemoryCache(new MemoryCacheOptions()))
        {
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

        public async Task<PagedList<JobIndexPosts>> SearchAsync(string? searchTerm, string? location, string? category, DateTime? postedAfter, DateTime? postedBefore, int page, int pageSize)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;

            var cacheKey = $"search:{searchTerm}|{location}|{category}|{postedAfter:O}|{postedBefore:O}|{page}|{pageSize}";
            if (_cache.TryGetValue<PagedList<JobIndexPosts>>(cacheKey, out var cached) && cached is not null)
            {
                return cached;
            }

            PagedList<JobIndexPosts> result;

            if (_db.Database.IsSqlServer())
            {
                var parts = new List<string>();
                if (!string.IsNullOrWhiteSpace(searchTerm)) parts.Add($"\"{searchTerm.Trim()}\"");
                if (!string.IsNullOrWhiteSpace(location)) parts.Add($"\"{location.Trim()}\"");
                if (!string.IsNullOrWhiteSpace(category)) parts.Add($"\"{category.Trim()}\"*");
                var ftQuery = parts.Count > 0 ? string.Join(" OR ", parts) : null;

                var off = (page - 1) * pageSize;
                var take = pageSize;

                if (!string.IsNullOrWhiteSpace(ftQuery))
                {
                    var baseSql = @"
SELECT j.*
FROM (
    SELECT j.JobID, t.[RANK]
    FROM CONTAINSTABLE(dbo.JobIndexPostingsExtended, (JobTitle, JobDescription, CompanyName, JobLocation), {0}) t
    JOIN dbo.JobIndexPostingsExtended j ON j.JobID = t.[KEY]
    UNION ALL
    SELECT j.JobID, tk.[RANK]
    FROM CONTAINSTABLE(dbo.JobKeywords, Keyword, {0}) tk
    JOIN dbo.JobKeywords k ON k.KeywordID = tk.[KEY]
    JOIN dbo.JobIndexPostingsExtended j ON j.JobID = k.JobID
) r
JOIN dbo.JobIndexPostingsExtended j ON j.JobID = r.JobID
WHERE 1=1";

                    if (postedAfter.HasValue)
                        baseSql += " AND j.Published >= {1}";
                    if (postedBefore.HasValue)
                        baseSql += " AND j.Published <= {2}";

                    baseSql += " ORDER BY r.[RANK] DESC, j.Published DESC OFFSET {3} ROWS FETCH NEXT {4} ROWS ONLY";

                    var itemsSql = await _db.JobIndexPosts
                        .FromSqlRaw(baseSql, ftQuery, postedAfter ?? (object)DBNull.Value, postedBefore ?? (object)DBNull.Value, off, take)
                        .Include(j => j.Categories)
                        .AsNoTracking()
                        .ToListAsync();

                    var countSql = @"
SELECT j.JobID
FROM CONTAINSTABLE(dbo.JobIndexPostingsExtended, (JobTitle, JobDescription, CompanyName, JobLocation), {0}) t
JOIN dbo.JobIndexPostingsExtended j ON j.JobID = t.[KEY]
UNION
SELECT j.JobID
FROM CONTAINSTABLE(dbo.JobKeywords, Keyword, {0}) tk
JOIN dbo.JobKeywords k ON k.KeywordID = tk.[KEY]
JOIN dbo.JobIndexPostingsExtended j ON j.JobID = k.JobID";

                    var totalCountSql = await _db.JobIndexPosts
                        .FromSqlRaw(countSql, ftQuery)
                        .Select(j => j.JobID)
                        .CountAsync();

                    result = new PagedList<JobIndexPosts>(totalCountSql, pageSize, page, itemsSql);
                }
                else
                {
                    var off2 = (page - 1) * pageSize;
                    var take2 = pageSize;
                    var baseSql = @"SELECT j.* FROM dbo.JobIndexPostingsExtended j WHERE 1=1";
                    if (postedAfter.HasValue)
                        baseSql += " AND j.Published >= {0}";
                    if (postedBefore.HasValue)
                        baseSql += " AND j.Published <= {1}";
                    baseSql += " ORDER BY j.Published DESC OFFSET {2} ROWS FETCH NEXT {3} ROWS ONLY";

                    var itemsSql = await _db.JobIndexPosts
                        .FromSqlRaw(baseSql, postedAfter ?? (object)DBNull.Value, postedBefore ?? (object)DBNull.Value, off2, take2)
                        .Include(j => j.Categories)
                        .AsNoTracking()
                        .ToListAsync();

                    var countSql = @"SELECT COUNT(1) FROM dbo.JobIndexPostingsExtended j WHERE 1=1";
                    if (postedAfter.HasValue)
                        countSql += " AND j.Published >= {0}";
                    if (postedBefore.HasValue)
                        countSql += " AND j.Published <= {1}";

                    var totalCountSql = await _db.JobIndexPosts
                        .FromSqlRaw(countSql, postedAfter ?? (object)DBNull.Value, postedBefore ?? (object)DBNull.Value)
                        .CountAsync();

                    result = new PagedList<JobIndexPosts>(totalCountSql, pageSize, page, itemsSql);
                }
            }
            else
            {
                // Fallback for testing/in-memory providers
                var q = _db.JobIndexPosts.Include(j => j.Categories).AsQueryable();
                if (postedAfter.HasValue) q = q.Where(j => j.Published >= postedAfter.Value);
                if (postedBefore.HasValue) q = q.Where(j => j.Published <= postedBefore.Value);

                if (!string.IsNullOrWhiteSpace(location))
                {
                    var loc = location.Trim().ToLowerInvariant();
                    q = q.Where(j => j.JobLocation != null && j.JobLocation.ToLower().Contains(loc));
                }
                if (!string.IsNullOrWhiteSpace(category))
                {
                    var cat = category.Trim().ToLowerInvariant();
                    q = q.Where(j => j.Categories.Any(c => c.Name != null && c.Name.ToLower().Contains(cat)));
                }
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    var term = searchTerm.Trim().ToLowerInvariant();
                    q = q.Where(j =>
                        (j.JobTitle != null && j.JobTitle.ToLower().Contains(term)) ||
                        (j.JobLocation != null && j.JobLocation.ToLower().Contains(term)) ||
                        (j.JobDescription != null && j.JobDescription.ToLower().Contains(term)) ||
                        (j.Categories.Any(c => c.Name != null && c.Name.ToLower().Contains(term))) ||
                        _db.JobKeywords.Any(k => k.JobID == j.JobID && k.Keyword != null && k.Keyword.ToLower().Contains(term))
                    );
                }

                var total = await q.CountAsync();
                if (total == 0)
                {
                    result = new PagedList<JobIndexPosts>(0, pageSize, page, []);
                }
                else
                {
                    var items = await q
                        .OrderByDescending(j => j.Published)
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize)
                        .AsNoTracking()
                        .ToListAsync();

                    result = new PagedList<JobIndexPosts>(total, pageSize, page, items);
                }
            }

            _cache.Set(cacheKey, result, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2)
            });

            return result;
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
                var rawCategoryData = await _db.Categories
                    .AsNoTracking()
                    .Select(c => new
                    {
                        c.CategoryID,
                        c.Name,
                        NumberOfJobs = c.JobIndexPosts.Count
                    })
                    .OrderBy(x => x.Name)
                    .ToListAsync();

                var categoryJobCounts = rawCategoryData
                    .Select(x => new CategoryJobCountResponse(x.CategoryID, x.Name, x.NumberOfJobs))
                    .ToList();

                return new CategoriesResponse(true, null, categoryJobCounts);
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

        public async Task<PagedList<JobIndexPosts>> GetRecommendedJobsByUserAndProfile(string userId, int page, int pageSize)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;

            var cacheKey = $"rec:{userId}:{page}:{pageSize}";
            if (_cache.TryGetValue<PagedList<JobIndexPosts>>(cacheKey, out var cached) && cached != null)
            {
                return cached;
            }

            PagedList<JobIndexPosts> result;

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
                return new PagedList<JobIndexPosts>(0, pageSize, page, []);

            if ((profile.Keywords == null || profile.Keywords.Count == 0)
                && (profile.Experiences == null || profile.Experiences.Count == 0)
                && (profile.Interests == null || profile.Interests.Count == 0)
                && (profile.BasicInfo == null || string.IsNullOrEmpty(profile.BasicInfo.JobTitle)))
                return new PagedList<JobIndexPosts>(0, pageSize, page, []);

            var keywords = GetKeywordsFromProfile(profile)
                .Where(k => !string.IsNullOrWhiteSpace(k))
                .Select(k => k.Trim())
                .ToList();

            if (keywords.Count == 0)
                return new PagedList<JobIndexPosts>(0, pageSize, page, []);

            if (_db.Database.IsSqlServer())
            {
                var ftQuery = string.Join(" OR ", keywords.Select(k => $"\"{k}\""));
                var off = (page - 1) * pageSize;
                var take = pageSize;

                var baseSqlRec = @"
SELECT j.*
FROM (
    SELECT j.JobID, t.[RANK]
    FROM CONTAINSTABLE(dbo.JobIndexPostingsExtended, (JobTitle, JobDescription, CompanyName, JobLocation), {0}) t
    JOIN dbo.JobIndexPostingsExtended j ON j.JobID = t.[KEY]
    UNION ALL
    SELECT j.JobID, tk.[RANK]
    FROM CONTAINSTABLE(dbo.JobKeywords, Keyword, {0}) tk
    JOIN dbo.JobKeywords k ON k.KeywordID = tk.[KEY]
    JOIN dbo.JobIndexPostingsExtended j ON j.JobID = k.JobID
) r
JOIN dbo.JobIndexPostingsExtended j ON j.JobID = r.JobID
ORDER BY r.[RANK] DESC, j.Published DESC
OFFSET {1} ROWS FETCH NEXT {2} ROWS ONLY";

                var items = await _db.JobIndexPosts
                    .FromSqlRaw(baseSqlRec, ftQuery, off, take)
                    .Include(j => j.Categories)
                    .AsNoTracking()
                    .ToListAsync();

                var countSqlRec = @"
SELECT j.JobID
FROM CONTAINSTABLE(dbo.JobIndexPostingsExtended, (JobTitle, JobDescription, CompanyName, JobLocation), {0}) t
JOIN dbo.JobIndexPostingsExtended j ON j.JobID = t.[KEY]
UNION
SELECT j.JobID
FROM CONTAINSTABLE(dbo.JobKeywords, Keyword, {0}) tk
JOIN dbo.JobKeywords k ON k.KeywordID = tk.[KEY]
JOIN dbo.JobIndexPostingsExtended j ON j.JobID = k.JobID";

                var totalCount = await _db.JobIndexPosts
                    .FromSqlRaw(countSqlRec, ftQuery)
                    .Select(j => j.JobID)
                    .CountAsync();

                result = new PagedList<JobIndexPosts>(totalCount, pageSize, page, items);
            }
            else
            {
                var kw = keywords.Select(k => k.ToLowerInvariant()).ToList();
                var q = _db.JobIndexPosts.Include(j => j.Categories).AsQueryable();

                q = q.Where(j =>
                    (j.JobTitle != null && kw.Any(k => j.JobTitle!.ToLower().Contains(k))) ||
                    (j.CompanyName != null && kw.Any(k => j.CompanyName!.ToLower().Contains(k))) ||
                    (j.JobDescription != null && kw.Any(k => j.JobDescription!.ToLower().Contains(k))) ||
                    (j.JobLocation != null && kw.Any(k => j.JobLocation!.ToLower().Contains(k))) ||
                    (j.Categories.Any(c => c.Name != null && kw.Any(k => c.Name.ToLower().Contains(k)))) ||
                    _db.JobKeywords.Any(kj => kj.JobID == j.JobID && kj.Keyword != null && kw.Any(k => kj.Keyword.ToLower().Contains(k)))
                );

                var total = await q.CountAsync();
                if (total == 0) return new PagedList<JobIndexPosts>(0, pageSize, page, []);

                var items = await q
                    .OrderByDescending(j => j.Published)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .AsNoTracking()
                    .ToListAsync();

                result = new PagedList<JobIndexPosts>(total, pageSize, page, items);
            }

            _cache.Set(cacheKey, result, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2)
            });

            return result;
        }

        private static HashSet<string> GetKeywordsFromProfile(Profile profile)
        {
            var keywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            profile.Keywords?.Where(kw => !string.IsNullOrWhiteSpace(kw))
                    .ToList()
                    .ForEach(kw => keywords.Add(kw));

            profile.Interests?.Where(i => !string.IsNullOrWhiteSpace(i.Title))
                    .ToList()
                    .ForEach(i => keywords.Add(i.Title));

            profile.Skills?.Where(s => !string.IsNullOrWhiteSpace(s.Name))
                    .ToList()
                    .ForEach(s => keywords.Add(s.Name));

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

            return keywords;
        }
    }
}