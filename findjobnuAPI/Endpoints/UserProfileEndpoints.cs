using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.OpenApi;
using findjobnuAPI.Models;
using findjobnuAPI.Services;
using System.Security.Claims;

namespace findjobnuAPI.Endpoints;

public static class UserProfileEndpoints
{
    public static void MapUserProfileEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/userprofile")
                          .WithTags(nameof(UserProfile))
                          .RequireAuthorization();

        group.MapGet("/{id}", async Task<Results<Ok<UserProfile>, NotFound>> (int id, IUserProfileService service) =>
        {
            var model = await service.GetByIdAsync(id);
            return model is not null ? TypedResults.Ok(model) : TypedResults.NotFound();
        })
        .WithName("GetUserProfileById")
        .WithOpenApi();

        group.MapPut("/{id}", async Task<Results<Ok, NotFound, ForbidHttpResult>> (int id, UserProfile userProfile, IUserProfileService service, HttpContext context) =>
        {
            var authenticatedUserId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (authenticatedUserId == null || authenticatedUserId != userProfile.UserId)
            {
                return TypedResults.Forbid();
            }
            var updated = await service.UpdateAsync(id, userProfile, authenticatedUserId);
            return updated ? TypedResults.Ok() : TypedResults.NotFound();
        })
        .WithName("UpdateUserProfile")
        .WithOpenApi();

        group.MapPost("/", async (UserProfile userProfile, IUserProfileService service) =>
        {
            var created = await service.CreateAsync(userProfile);
            return TypedResults.Created($"/api/UserProfile/{created!.Id}", created);
        })
        .WithName("CreateUserProfile")
        .WithOpenApi();
    }
}
