using findjobnuAPI.Models;
using findjobnuAPI.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace findjobnuAPI.Endpoints;

public static class WorkProfileEndpoints
{
    public static void MapWorkProfileEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/workprofile")
            .WithTags(nameof(WorkProfile))
            .RequireAuthorization();

        group.MapGet("/user/{userProfileId:int}", async Task<Results<Ok<WorkProfile>, NotFound>> (int userProfileId, IWorkProfileService service) =>
        {
            var model = await service.GetByUserProfileIdAsync(userProfileId);
            return model is not null ? TypedResults.Ok(model) : TypedResults.NotFound();
        })
        .WithName("GetWorkProfileByUserProfileId")
        .WithOpenApi();

        group.MapPost("/", async (WorkProfile workProfile, IWorkProfileService service, HttpContext context) =>
        {
            var authenticatedUserId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (authenticatedUserId == null || workProfile.UserProfile.UserId != authenticatedUserId)
                return Results.Forbid();
            var created = await service.CreateAsync(workProfile);
            return Results.Created($"/api/workprofile/user/{created!.UserProfileId}", created);
        })
        .WithName("CreateWorkProfile")
        .WithOpenApi();

        group.MapPut("/{id:int}", async (int id, WorkProfile workProfile, IWorkProfileService service, HttpContext context) =>
        {
            var authenticatedUserId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (authenticatedUserId == null || workProfile.UserProfile == null || workProfile.UserProfile.UserId != authenticatedUserId)
                return Results.Forbid();
            var updated = await service.UpdateAsync(id, workProfile, authenticatedUserId);
            return updated ? Results.Ok() : Results.NotFound();
        })
        .WithName("UpdateWorkProfile")
        .WithOpenApi();

        group.MapDelete("/{id:int}", async (int id, IWorkProfileService service, HttpContext context) =>
        {
            var authenticatedUserId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (authenticatedUserId == null)
                return Results.Forbid();
            var deleted = await service.DeleteAsync(id, authenticatedUserId);
            return deleted ? Results.Ok() : Results.NotFound();
        })
        .WithName("DeleteWorkProfile")
        .WithOpenApi();
    }
}
