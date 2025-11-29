using FindjobnuService.Models;
using Microsoft.AspNetCore.Http.HttpResults;

namespace FindjobnuService.Services
{
    public interface IJobIndexPostsService
    {
        Task<PagedList<JobIndexPosts>> GetAllAsync(int page, int pageSize);
        Task<PagedList<JobIndexPosts>> SearchAsync(string? searchTerm, string? location, string? category, DateTime? postedAfter, DateTime? postedBefore, int page);
        Task<JobIndexPosts> GetByIdAsync(int id);
        Task<CategoriesResponse> GetCategoriesAsync();
        Task<PagedList<JobIndexPosts>> GetSavedJobsByUserId(string userid, int page);
        Task<PagedList<JobIndexPosts>> GetRecommendedJobsByUserAndProfile(string userId, int page);
    }
}
