using AuthService.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AuthService.Endpoints;

public static class LinkedInAuthEndpoints
{
    public static void MapLinkedInAuthEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/auth/linkedin").WithTags("LinkedInAuth");

        group.MapGet("/login", ([FromServices] ILinkedInAuthService linkedInAuthService) =>
        {
            var authUrl = linkedInAuthService.GetLoginUrl();
            return Results.Redirect(authUrl);
        })
        .WithName("LinkedInLogin");

        group.MapGet("/callback", async (HttpContext context, [FromServices] ILinkedInAuthService linkedInAuthService) =>
        {
            return await linkedInAuthService.HandleCallbackAsync(context);
        })
        .WithName("LinkedInCallback");

        group.MapPost("/unlink", async (HttpContext context, [FromServices] ILinkedInAuthService linkedInAuthService) =>
        {
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Results.BadRequest("User ID not found in claims.");
            }

            return await linkedInAuthService.UnlinkLinkedInProfile(userId);
        })
        .WithName("UnlinkLinkedInProfile")
        .RequireAuthorization()
        .WithSummary("Unlinks a LinkedIn profile from the user's account.")
        .WithDescription("Removes the LinkedIn profile association for the specified user ID.")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest);
    }
}
