using findjobnuAPI.Models;
using findjobnuAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.OpenApi;
using System.Security.Claims;

namespace findjobnuAPI.Endpoints;

public static class JobIndexPostsEndpoints
{
    public static void MapJobIndexPostsEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/jobindexposts").WithTags(nameof(JobIndexPosts));

        group.MapGet("/", async (IJobIndexPostsService service, int page = 1, int pageSize = 10) =>
        {
            var pagedList = await service.GetAllAsync(page, pageSize);
            return TypedResults.Ok(pagedList);
        })
        .WithName("GetAllJobPosts")
        .WithOpenApi();

        group.MapGet("/search", async Task<Results<Ok<PagedList<JobIndexPosts>>, NoContent>> (
            [AsParameters] JobIndexPostsSearchRequest request,
            IJobIndexPostsService service) =>
        {
            var pagedList = await service.SearchAsync(
                request.SearchTerm,
                request.Location,
                request.Category,
                request.PostedAfter,
                request.PostedBefore,
                request.Page);

            return pagedList?.Items.Any() == true ? TypedResults.Ok(pagedList) : TypedResults.NoContent();
        })
        .WithName("GetJobPostsBySearch")
        .WithOpenApi();

        group.MapGet("/{id}", async Task<Results<Ok<JobIndexPosts>, NoContent>> (int id, IJobIndexPostsService service) =>
        {
            var model = await service.GetByIdAsync(id);
            return model != null ? TypedResults.Ok(model) : TypedResults.NoContent();
        })
        .WithName("GetJobPostsById")
        .WithOpenApi();

        group.MapGet("/categories", async (IJobIndexPostsService service) =>
        {
            var categories = await service.GetCategoriesAsync();
            return TypedResults.Ok(categories);
        })
        .WithName("GetJobCategories")
        .WithOpenApi();

        group.MapGet("/saved", async Task<Results<Ok<List<JobIndexPosts>>, UnauthorizedHttpResult>> (HttpContext httpContext, IJobIndexPostsService service) =>
        {
            var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return TypedResults.Unauthorized();

            var pagedList = await service.GetSavedJobsByUserId(userId);
            return TypedResults.Ok(pagedList);
        })
        .RequireAuthorization()
        .WithName("GetSavedJobPostsByUser")
        .WithOpenApi();
    }
}
