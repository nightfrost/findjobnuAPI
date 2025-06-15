using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.OpenApi;
using findjobnuAPI.Models;
using findjobnuAPI.Repositories.Context;
using Microsoft.IdentityModel.Tokens;
namespace findjobnuAPI.Endpoints;

public static class JobIndexPostsEndpoints
{
    public static void MapJobIndexPostsEndpoints (this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/jobindexposts").WithTags(nameof(JobIndexPosts));

        group.MapGet("/", async (FindjobnuContext db, int page = 1, int pageSize = 10) =>
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;

            var totalCount = await db.JobIndexPosts.CountAsync();
            var items = await db.JobIndexPosts
                .OrderBy(j => j.JobID)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();

            var pagedList = new PagedList<JobIndexPosts>(totalCount, pageSize, page, items);
            return TypedResults.Ok(pagedList);
        })
        .WithName("GetAllJobPosts")
        .WithOpenApi();

        group.MapGet("/search", async Task<Results<Ok<PagedList<JobIndexPosts>>, NoContent>> (string? searchTerm, 
            string? location, 
            string? category, 
            DateTime? postedAfter, 
            DateTime? postedBefore, 
            FindjobnuContext db, 
            int page = 1) =>
        {
            var pageSize = 20;
            var totalCount = await db.JobIndexPosts
                .OrderBy(j => j.JobID)
                .Where(j => !string.IsNullOrEmpty(location) ? j.JobLocation != null && j.JobLocation.Contains(location) : true)
                .Where(j => !string.IsNullOrEmpty(category) ? j.Category != null && j.Category.Contains(category) : true)
                .Where(j => !string.IsNullOrEmpty(searchTerm) ? j.JobTitle != null && j.JobTitle.Contains(searchTerm) : true)
                .Where(j => postedAfter.HasValue ? j.Published >= postedAfter.Value : true)
                .Where(j => postedBefore.HasValue ? j.Published <= postedBefore.Value : true)
                .AsNoTracking()
                .CountAsync();
            var items = await db.JobIndexPosts
                .OrderBy(j => j.JobID)
                .Where(j => !string.IsNullOrEmpty(location) ? j.JobLocation != null && j.JobLocation.Contains(location) : true)
                .Where(j => !string.IsNullOrEmpty(category) ? j.Category != null && j.Category.Contains(category) : true)
                .Where(j => !string.IsNullOrEmpty(searchTerm) ? j.JobTitle != null && j.JobTitle.Contains(searchTerm) : true)
                .Where(j => postedAfter.HasValue ? j.Published >= postedAfter.Value : true)
                .Where(j => postedBefore.HasValue ? j.Published <= postedBefore.Value : true)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();

            var pagedList = new PagedList<JobIndexPosts>(totalCount, pageSize, page, items);
            return pagedList.Items.Any() ? TypedResults.Ok(pagedList) : TypedResults.NoContent();
        })
        .WithName("GetJobPostsBySearch")
        .WithOpenApi();

        group.MapGet("/{id}", async Task<Results<Ok<JobIndexPosts>, NotFound>> (int jobid, FindjobnuContext db) =>
        {
            return await db.JobIndexPosts.AsNoTracking()
                .FirstOrDefaultAsync(model => model.JobID == jobid)
                is JobIndexPosts model
                    ? TypedResults.Ok(model)
                    : TypedResults.NotFound();
        })
        .WithName("GetJobPostsById")
        .WithOpenApi();
    }
}
