using FindjobnuService.Models;

namespace FindjobnuService.Services
{
    public interface IJobIndexPostsService
    {
        Task<PagedList<JobIndexPosts>> GetAllAsync(int page, int pageSize);
        Task<PagedList<JobIndexPosts>> SearchAsync(string? searchTerm, string? location, string? category, DateTime? postedAfter, DateTime? postedBefore, int page, int pageSize);
        Task<JobIndexPosts> GetByIdAsync(int id);
        Task<CategoriesResponse> GetCategoriesAsync();
        Task<PagedList<JobIndexPosts>> GetSavedJobsByUserId(string userid, int page);
        Task<PagedList<JobIndexPosts>> GetRecommendedJobsByUserAndProfile(string userId, int page, int pageSize);
    }
}
