using findjobnuAPI.Services;
using findjobnuAPI.Models;
using findjobnuAPI.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.OpenApi;
using System.Security.Claims;

namespace findjobnuAPI
{
    public static class ProfileEndpoints
    {
        public static void MapProfileEndpoints(this IEndpointRouteBuilder routes)
        {
            var group = routes.MapGroup("/api/profile").WithTags("Profile").RequireAuthorization();

            group.MapGet("/{userId}", async Task<Results<Ok<ProfileDto>, UnauthorizedHttpResult, NotFound>> (string userId, HttpContext ctx, IProfileService service) =>
            {
                var authedUserId = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(authedUserId) || authedUserId != userId)
                    return TypedResults.Unauthorized();
                var profile = await service.GetByUserIdAsync(userId);
                return profile != null ? TypedResults.Ok(profile) : TypedResults.NotFound();
            })
            .WithName("GetProfileByUserId")
            .WithOpenApi();

            group.MapPost("/", async Task<Results<Ok<Profile>, ForbidHttpResult, BadRequest<string>>> (Profile profile, HttpContext ctx, IProfileService service) =>
            {
                var authedUserId = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(authedUserId) || authedUserId != profile.UserId)
                    return TypedResults.Forbid();
                var created = await service.CreateAsync(profile);
                return created != null ? TypedResults.Ok(created) : TypedResults.BadRequest("Could not create profile");
            })
            .WithName("CreateProfile")
            .WithOpenApi();

            group.MapPut("/{id}", async Task<Results<Ok, ForbidHttpResult>> (int id, Profile profile, HttpContext ctx, IProfileService service) =>
            {
                var authedUserId = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(authedUserId) || authedUserId != profile.UserId)
                    return TypedResults.Forbid();
                var ok = await service.UpdateAsync(id, profile, authedUserId);
                return ok ? TypedResults.Ok() : TypedResults.Forbid();
            })
            .WithName("UpdateProfile")
            .WithOpenApi();

            group.MapGet("/{userId}/savedjobs", async Task<Results<Ok<PagedList<JobIndexPosts>>, UnauthorizedHttpResult>> (string userId, HttpContext ctx, IProfileService service) =>
            {
                var authedUserId = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(authedUserId) || authedUserId != userId)
                    return TypedResults.Unauthorized();
                var jobs = await service.GetSavedJobsByUserIdAsync(userId);
                return TypedResults.Ok(jobs);
            })
            .WithName("GetSavedJobsByUserId")
            .WithOpenApi();

            group.MapPost("/{userId}/savedjobs/{jobId}", async Task<Results<Ok, ForbidHttpResult, BadRequest<string>>> (string userId, string jobId, HttpContext ctx, IProfileService service) =>
            {
                var authedUserId = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(authedUserId) || authedUserId != userId)
                    return TypedResults.Forbid();
                var ok = await service.SaveJobAsync(userId, jobId);
                return ok ? TypedResults.Ok() : TypedResults.BadRequest("Could not save job");
            })
            .WithName("SaveJobForUser")
            .WithOpenApi();

            group.MapDelete("/{userId}/savedjobs/{jobId}", async Task<Results<Ok, ForbidHttpResult, BadRequest<string>>> (string userId, string jobId, HttpContext ctx, IProfileService service) =>
            {
                var authedUserId = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(authedUserId) || authedUserId != userId)
                    return TypedResults.Forbid();
                var ok = await service.RemoveSavedJobAsync(userId, jobId);
                return ok ? TypedResults.Ok() : TypedResults.BadRequest("Could not remove saved job");
            })
            .WithName("RemoveSavedJobForUser")
            .WithOpenApi();

            group.MapPost("/linkedin/import", async Task<Results<Ok<Profile>, UnauthorizedHttpResult, BadRequest<string>>> (HttpContext ctx, ILinkedInProfileService linkedInService) =>
            {
                var userId = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return TypedResults.Unauthorized();
                var result = await linkedInService.GetProfileAsync(userId);
                if (!result.Success)
                    return TypedResults.BadRequest(result.Error);
                var saved = await linkedInService.SaveProfileAsync(userId, result.Profile!);
                return saved ? TypedResults.Ok(result.Profile) : TypedResults.BadRequest("Failed to save imported profile");
            })
            .WithName("ImportLinkedInProfile")
            .WithOpenApi();

            group.MapGet("/{userId}/basicinfo", async Task<Results<Ok<BasicInfoDto>, ForbidHttpResult, NotFound>> (string userId, HttpContext ctx, IProfileService service) =>
            {
                var authedUserId = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(authedUserId) || authedUserId != userId)
                    return TypedResults.Forbid();
                var dto = await service.GetProfileBasicInfoByUserIdAsync(userId);
                if (dto == null)
                    return TypedResults.NotFound();
                return TypedResults.Ok(dto);
            })
            .WithName("GetBasicInfoByUserId")
            .WithOpenApi();

            group.MapGet("/{userId}/experience", async Task<Results<Ok<List<ExperienceDto>>, ForbidHttpResult, NotFound>> (string userId, HttpContext ctx, IProfileService service) =>
            {
                var authedUserId = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(authedUserId) || authedUserId != userId)
                    return TypedResults.Forbid();
                var dtos = await service.GetProfileExperienceByUserIdAsync(userId);
                if (dtos == null || dtos.Count == 0)
                    return TypedResults.NotFound();
                return TypedResults.Ok(dtos);
            })
            .WithName("GetExperienceByUserId")
            .WithOpenApi();

            group.MapGet("/{userId}/skills", async Task<Results<Ok<List<SkillDto>>, ForbidHttpResult, NotFound>> (string userId, HttpContext ctx, IProfileService service) =>
            {
                var authedUserId = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(authedUserId) || authedUserId != userId)
                    return TypedResults.Forbid();
                var dtos = await service.GetProfileSkillsByUserIdAsync(userId);
                if (dtos == null || dtos.Count == 0)
                    return TypedResults.NotFound();
                return TypedResults.Ok(dtos);
            })
            .WithName("GetSkillsByUserId")
            .WithOpenApi();

            group.MapGet("/{userId}/education", async Task<Results<Ok<List<EducationDto>>, ForbidHttpResult, NotFound>> (string userId, HttpContext ctx, IProfileService service) =>
            {
                var authedUserId = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(authedUserId) || authedUserId != userId)
                    return TypedResults.Forbid();
                var dtos = await service.GetProfileEducationByUserIdAsync(userId);
                if (dtos == null || dtos.Count == 0)
                    return TypedResults.NotFound();
                return TypedResults.Ok(dtos);
            })
            .WithName("GetEducationByUserId")
            .WithOpenApi();
        }
    }
}
