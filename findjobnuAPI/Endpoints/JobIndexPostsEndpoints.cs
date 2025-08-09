using findjobnuAPI.Models;
using findjobnuAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace findjobnuAPI.Endpoints;

public static class JobIndexPostsEndpoints
{
    public static void MapJobIndexPostsEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/jobindexposts").WithTags(nameof(JobIndexPosts));

        group.MapGet("/", async Task<Results<Ok<PagedList<JobIndexPosts>>, NoContent>> (
            [FromServices] IJobIndexPostsService service, 
            [FromQuery] int page = 1, 
            [FromQuery] int pageSize = 10) =>
        {
            var pagedList = await service.GetAllAsync(page, pageSize);
            return pagedList.Items.Any() ? TypedResults.Ok(pagedList) : TypedResults.NoContent();
        })
        .WithName("GetAllJobPosts")
        .WithOpenApi();

        group.MapGet("/search", async Task<Results<Ok<PagedList<JobIndexPosts>>, NoContent>> (
            [AsParameters] JobIndexPostsSearchRequest request,
            [FromServices] IJobIndexPostsService service) =>
        {
            var pagedList = await service.SearchAsync(
                request.SearchTerm,
                request.Location,
                request.Category,
                request.PostedAfter,
                request.PostedBefore,
                request.Page);

            return pagedList.Items.Any() ? TypedResults.Ok(pagedList) : TypedResults.NoContent();
        })
        .WithName("GetJobPostsBySearch")
        .WithOpenApi();

        group.MapGet("/{id}", async Task<Results<Ok<JobIndexPosts>, NoContent>> (int id, [FromServices] IJobIndexPostsService service) =>
        {
            var jobPost = await service.GetByIdAsync(id);

            return !jobPost.JobUrl.IsNullOrEmpty() ? TypedResults.Ok(jobPost) : TypedResults.NoContent();
        })
        .WithName("GetJobPostsById")
        .WithOpenApi();

        group.MapGet("/categories", async Task<Results<Ok<CategoriesResponse>, NoContent>> ([FromServices] IJobIndexPostsService service) =>
        {
            var categories = await service.GetCategoriesAsync();
            return categories.CategoryAndAmountOfJobs.Count > 0 ? TypedResults.Ok(categories) : TypedResults.NoContent();
        })
        .WithName("GetJobCategories")
        .WithOpenApi();

        group.MapGet("/saved", async Task<Results<Ok<PagedList<JobIndexPosts>>, UnauthorizedHttpResult, NoContent>> (
            [FromQuery] int page, 
            HttpContext httpContext, 
            [FromServices] IJobIndexPostsService service) =>
        {
            var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return TypedResults.Unauthorized();

            var pagedList = await service.GetSavedJobsByUserId(userId, page);
            return pagedList?.Items.Count() >= 0 ? TypedResults.Ok(pagedList) :
                TypedResults.NoContent();
        })
        .RequireAuthorization()
        .WithName("GetSavedJobPostsByUser")
        .WithOpenApi();

        group.MapGet("/recommended-jobs", async Task<Results<Ok<PagedList<JobIndexPosts>>, UnauthorizedHttpResult, BadRequest<string>, NoContent>> (
            [FromQuery] int page, 
            HttpContext httpContext, 
            [FromServices] IJobIndexPostsService jobService, 
            [FromServices] IProfileService profileService) =>
        {
            var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return TypedResults.Unauthorized();
            var profile = await profileService.GetByUserIdAsync(userId);
            if (profile == null)
                return TypedResults.BadRequest("No Profile setup.");

            var pagedList = await jobService.GetRecommendedJobsByUserAndProfile(profile, page);
            return pagedList?.Items.Any() == true ? TypedResults.Ok(pagedList) :
                TypedResults.NoContent();
        })
        .RequireAuthorization()
        .WithName("GetRecommendedJobsForUser")
        .WithOpenApi();
    }
}
