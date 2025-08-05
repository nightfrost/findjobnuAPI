using Microsoft.EntityFrameworkCore;
using findjobnuAPI.Models;
using findjobnuAPI.Repositories.Context;
using Humanizer.Localisation.DateToOrdinalWords;

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

        public async Task<List<string>> GetCategoriesAsync()
        {
            return await _db.Categories
                .AsNoTracking()
                .Select(c => c.Name)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();
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
    }
}