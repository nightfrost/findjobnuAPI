using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using FindjobnuService.Models;
using FindjobnuService.Services;
using DTORequest = FindjobnuService.DTOs.Requests.JobIndexPostsSearchRequest;
using FindjobnuService.DTOs.Responses;
using FindjobnuService.Mappers;

namespace FindjobnuService.Endpoints;

public static class JobIndexPostsEndpoints
{
    public static void MapJobIndexPostsEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/jobindexposts").WithTags(nameof(JobIndexPosts));

        group.MapGet("/", async Task<Results<Ok<PagedResponse<JobIndexPostResponse>>, NoContent>> (
            [FromServices] IJobIndexPostsService service, 
            [FromQuery] int page = 1, 
            [FromQuery] int pageSize = 10) =>
        {
            var pagedList = await service.GetAllAsync(page, pageSize);
            var dto = JobIndexPostsMapper.ToPagedDto(pagedList);
            return dto.Items.Any() ? TypedResults.Ok(dto) : TypedResults.NoContent();
        })
        .WithName("GetAllJobPosts")
        .WithOpenApi();

        group.MapGet("/search", async Task<Results<Ok<PagedResponse<JobIndexPostResponse>>, NoContent>> (
            [AsParameters] DTORequest request,
            [FromServices] IJobIndexPostsService service) =>
        {
            var pagedList = await service.SearchAsync(
                request.SearchTerm,
                request.Location,
                request.Category,
                request.PostedAfter,
                request.PostedBefore,
                request.Page);

            var dto = JobIndexPostsMapper.ToPagedDto(pagedList);
            return dto.Items.Any() ? TypedResults.Ok(dto) : TypedResults.NoContent();
        })
        .WithName("GetJobPostsBySearch")
        .WithOpenApi();

        group.MapGet("/{id}", async Task<Results<Ok<JobIndexPostResponse>, NoContent>> (int id, [FromServices] IJobIndexPostsService service) =>
        {
            var jobPost = await service.GetByIdAsync(id);

            return !jobPost.JobUrl.IsNullOrEmpty() ? TypedResults.Ok(JobIndexPostsMapper.ToDto(jobPost)) : TypedResults.NoContent();
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

        group.MapGet("/saved", async Task<Results<Ok<PagedResponse<JobIndexPostResponse>>, UnauthorizedHttpResult, NoContent>> (
            [FromQuery] int page, 
            HttpContext httpContext, 
            [FromServices] IJobIndexPostsService service) =>
        {
            var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return TypedResults.Unauthorized();

            var pagedList = await service.GetSavedJobsByUserId(userId, page);
            var dto = JobIndexPostsMapper.ToPagedDto(pagedList!);
            return dto.Items.Count >= 0 ? TypedResults.Ok(dto) :
                TypedResults.NoContent();
        })
        .RequireAuthorization()
        .WithName("GetSavedJobPostsByUser")
        .WithOpenApi();

        group.MapGet("/recommended-jobs", async Task<Results<Ok<PagedResponse<JobIndexPostResponse>>, UnauthorizedHttpResult, BadRequest<string>, NoContent>> (
            [FromQuery] int page, 
            HttpContext httpContext, 
            [FromServices] IJobIndexPostsService jobService, 
            [FromServices] IProfileService profileService ) =>
        {
            var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return TypedResults.Unauthorized();

            var pagedList = await jobService.GetRecommendedJobsByUserAndProfile(userId, page);
            var dto = JobIndexPostsMapper.ToPagedDto(pagedList!);
            return dto.Items.Any() ? TypedResults.Ok(dto) :
                TypedResults.NoContent();
        })
        .RequireAuthorization()
        .WithName("GetRecommendedJobsForUser")
        .WithOpenApi();
    }
}
