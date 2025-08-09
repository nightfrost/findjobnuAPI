using findjobnuAPI.Services;
using findjobnuAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.OpenApi;
using System.Security.Claims;

namespace findjobnuAPI
{
    public record BasicInfoDto(string FirstName, string LastName, DateTime? DateOfBirth, string? PhoneNumber, string? About, string? Location, string? Company, string? JobTitle, string? LinkedinUrl, bool OpenToWork);
    public record ExperienceDto(int Id, string? PositionTitle, string? Company, string? FromDate, string? ToDate, string? Duration, string? Location, string? Description, string? LinkedinUrl);
    public record EducationDto(int Id, string? Institution, string? Degree, string? FromDate, string? ToDate, string? Description, string? LinkedinUrl);
    public record SkillDto(int Id, string Name, SkillProficiency Proficiency);

    public static class ProfileEndpoints
    {
        public static void MapProfileEndpoints(this IEndpointRouteBuilder routes)
        {
            var group = routes.MapGroup("/api/profile").WithTags("Profile").RequireAuthorization();

            group.MapGet("/{userId}", async Task<Results<Ok<Profile>, ForbidHttpResult, NotFound>> (string userId, HttpContext ctx, IProfileService service) =>
            {
                var authedUserId = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(authedUserId) || authedUserId != userId)
                    return TypedResults.Forbid();
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

            group.MapGet("/{userId}/savedjobs", async Task<Results<Ok<List<string>>, ForbidHttpResult>> (string userId, HttpContext ctx, IProfileService service) =>
            {
                var authedUserId = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(authedUserId) || authedUserId != userId)
                    return TypedResults.Forbid();
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
                var profile = await service.GetByUserIdAsync(userId);
                if (profile == null)
                    return TypedResults.NotFound();
                var b = profile.BasicInfo;
                var dto = new BasicInfoDto(b.FirstName, b.LastName, b.DateOfBirth, b.PhoneNumber, b.About, b.Location, b.Company, b.JobTitle, b.LinkedinUrl, b.OpenToWork);
                return TypedResults.Ok(dto);
            })
            .WithName("GetBasicInfoByUserId")
            .WithOpenApi();

            group.MapGet("/{userId}/experience", async Task<Results<Ok<List<ExperienceDto>>, ForbidHttpResult, NotFound>> (string userId, HttpContext ctx, IProfileService service) =>
            {
                var authedUserId = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(authedUserId) || authedUserId != userId)
                    return TypedResults.Forbid();
                var profile = await service.GetByUserIdAsync(userId);
                if (profile == null)
                    return TypedResults.NotFound();
                var dtos = profile.Experiences?.Select(e => new ExperienceDto(e.Id, e.PositionTitle, e.Company, e.FromDate, e.ToDate, e.Duration, e.Location, e.Description, e.LinkedinUrl)).ToList() ?? [];
                return TypedResults.Ok(dtos);
            })
            .WithName("GetExperienceByUserId")
            .WithOpenApi();

            group.MapGet("/{userId}/skills", async Task<Results<Ok<List<SkillDto>>, ForbidHttpResult, NotFound>> (string userId, HttpContext ctx, IProfileService service) =>
            {
                var authedUserId = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(authedUserId) || authedUserId != userId)
                    return TypedResults.Forbid();
                var profile = await service.GetByUserIdAsync(userId);
                if (profile == null)
                    return TypedResults.NotFound();
                var dtos = profile.Skills?.Select(s => new SkillDto(s.Id, s.Name, s.Proficiency)).ToList() ?? [];
                return TypedResults.Ok(dtos);
            })
            .WithName("GetSkillsByUserId")
            .WithOpenApi();

            group.MapGet("/{userId}/education", async Task<Results<Ok<List<EducationDto>>, ForbidHttpResult, NotFound>> (string userId, HttpContext ctx, IProfileService service) =>
            {
                var authedUserId = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(authedUserId) || authedUserId != userId)
                    return TypedResults.Forbid();
                var profile = await service.GetByUserIdAsync(userId);
                if (profile == null)
                    return TypedResults.NotFound();
                var dtos = profile.Educations?.Select(e => new EducationDto(e.Id, e.Institution, e.Degree, e.FromDate, e.ToDate, e.Description, e.LinkedinUrl)).ToList() ?? [];
                return TypedResults.Ok(dtos);
            })
            .WithName("GetEducationByUserId")
            .WithOpenApi();
        }
    }
}
