using Microsoft.EntityFrameworkCore;
using findjobnuAPI.Models;
using findjobnuAPI.Repositories.Context;

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
            var query = _db.JobIndexPosts.AsQueryable();

            if (!string.IsNullOrEmpty(location))
                query = query.Where(j => j.JobLocation != null && j.JobLocation.Contains(location));
            if (!string.IsNullOrEmpty(category))
                query = query.Where(j => j.Category != null && j.Category.Contains(category));
            if (!string.IsNullOrEmpty(searchTerm))
                query = query.Where(j => j.JobTitle != null && j.JobTitle.Contains(searchTerm));
            if (postedAfter.HasValue)
                query = query.Where(j => j.Published >= postedAfter.Value);
            if (postedBefore.HasValue)
                query = query.Where(j => j.Published <= postedBefore.Value);

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderBy(j => j.JobID)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();

            if (!items.Any()) return null;
            return new PagedList<JobIndexPosts>(totalCount, pageSize, page, items);
        }

        public async Task<JobIndexPosts?> GetByIdAsync(int id)
        {
            return await _db.JobIndexPosts.AsNoTracking()
                .FirstOrDefaultAsync(model => model.JobID == id);
        }

        public async Task<List<string>> GetCategoriesAsync()
        {
            return await _db.JobIndexPosts
                .Where(j => !string.IsNullOrEmpty(j.Category))
                .AsNoTracking()
                .Select(j => j.Category!)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();
        }

        public async Task<List<JobIndexPosts>> GetSavedJobsByUserId(string userid)
        {
            var userSavedJobIds = await _db.UserProfile
                .Where(up => up.UserId == userid)
                .SelectMany(up => up.SavedJobPosts ?? new List<string>())
                .ToListAsync();

            if (userSavedJobIds == null || userSavedJobIds.Count == 0)
                return new List<JobIndexPosts>();

            var jobIds = userSavedJobIds
                .Select(id => int.TryParse(id, out var intId) ? (int?)intId : null)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .ToList();

            var query = _db.JobIndexPosts
                .Where(j => jobIds.Contains(j.JobID))
                .AsNoTracking();

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderBy(j => j.JobID)
                .ToListAsync();

            return [.. items];
        }
    }
}