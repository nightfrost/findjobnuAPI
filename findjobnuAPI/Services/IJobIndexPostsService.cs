using findjobnuAPI.Models;

namespace findjobnuAPI.Services
{
    public interface IJobIndexPostsService
    {
        Task<PagedList<JobIndexPosts>> GetAllAsync(int page, int pageSize);
        Task<PagedList<JobIndexPosts>?> SearchAsync(string? searchTerm, string? location, string? category, DateTime? postedAfter, DateTime? postedBefore, int page);
        Task<JobIndexPosts?> GetByIdAsync(int id);
        Task<List<string>> GetCategoriesAsync();
        Task<PagedList<JobIndexPosts>> GetSavedJobsByUserId(string userid, int page);
    }
}
