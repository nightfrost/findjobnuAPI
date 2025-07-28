using findjobnuAPI.Models;
using findjobnuAPI.Models.LinkedIn;
using findjobnuAPI.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.OpenApi;
using System.Security.Claims;

namespace findjobnuAPI.Endpoints
{
    public static class LinkedInEndpoints
    {
        public static void MapLinkedInEndpoints(this IEndpointRouteBuilder routes)
        {
            var group = routes.MapGroup("/api/linkedin")
                          .WithTags("LinkedIn Integration")
                          .RequireAuthorization();

            // Get LinkedIn authorization URL
            group.MapGet("/auth-url", (string redirectUri, string? state, ILinkedInService linkedInService) =>
            {
                var authUrl = linkedInService.GetAuthorizationUrl(redirectUri, state ?? Guid.NewGuid().ToString());
                return TypedResults.Ok(new { AuthorizationUrl = authUrl });
            })
            .WithName("GetLinkedInAuthUrl")
            .WithOpenApi()
            .AllowAnonymous(); // Allow anonymous access for getting auth URL

            // Connect LinkedIn account
            group.MapPost("/connect", async (
                LinkedInConnectRequest request, 
                ILinkedInService linkedInService, 
                HttpContext context) =>
            {
                var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Results.Unauthorized();
                }

                if (string.IsNullOrEmpty(request.AuthorizationCode) || string.IsNullOrEmpty(request.RedirectUri))
                {
                    return Results.BadRequest("Authorization code and redirect URI are required");
                }

                var success = await linkedInService.ConnectLinkedInAccountAsync(userId, request.AuthorizationCode, request.RedirectUri);
                
                if (success)
                {
                    return Results.Ok(new { Message = "LinkedIn account connected successfully" });
                }
                else
                {
                    return Results.BadRequest("Failed to connect LinkedIn account");
                }
            })
            .WithName("ConnectLinkedInAccount")
            .WithOpenApi();

            // Get LinkedIn profile
            group.MapGet("/profile", async (
                ILinkedInService linkedInService, 
                HttpContext context) =>
            {
                var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Results.Unauthorized();
                }

                var profile = await linkedInService.GetLinkedInProfileAsync(userId);
                return profile != null ? Results.Ok(profile) : Results.NotFound();
            })
            .WithName("GetLinkedInProfile")
            .WithOpenApi();

            // Sync LinkedIn profile
            group.MapPost("/sync", async (
                LinkedInSyncRequest? request,
                ILinkedInService linkedInService, 
                HttpContext context) =>
            {
                var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Results.Unauthorized();
                }

                var forceRefresh = request?.ForceRefresh ?? false;
                var success = await linkedInService.SyncLinkedInProfileAsync(userId, forceRefresh);
                
                if (success)
                {
                    return Results.Ok(new { Message = "LinkedIn profile synced successfully" });
                }
                else
                {
                    return Results.BadRequest("Failed to sync LinkedIn profile. Please ensure your LinkedIn account is connected.");
                }
            })
            .WithName("SyncLinkedInProfile")
            .WithOpenApi();

            // Disconnect LinkedIn account
            group.MapDelete("/disconnect", async (
                ILinkedInService linkedInService, 
                HttpContext context) =>
            {
                var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Results.Unauthorized();
                }

                var success = await linkedInService.DisconnectLinkedInAccountAsync(userId);
                return Results.Ok(new { Message = success ? "LinkedIn account disconnected successfully" : "No LinkedIn account found to disconnect" });
            })
            .WithName("DisconnectLinkedInAccount")
            .WithOpenApi();

            // Get work experience
            group.MapGet("/work-experience", async (
                ILinkedInService linkedInService, 
                HttpContext context) =>
            {
                var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Results.Unauthorized();
                }

                var workExperience = await linkedInService.GetWorkExperienceAsync(userId);
                return workExperience != null ? Results.Ok(workExperience) : Results.NotFound();
            })
            .WithName("GetLinkedInWorkExperience")
            .WithOpenApi();

            // Get education
            group.MapGet("/education", async (
                ILinkedInService linkedInService, 
                HttpContext context) =>
            {
                var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Results.Unauthorized();
                }

                var education = await linkedInService.GetEducationAsync(userId);
                return education != null ? Results.Ok(education) : Results.NotFound();
            })
            .WithName("GetLinkedInEducation")
            .WithOpenApi();

            // Get skills
            group.MapGet("/skills", async (
                ILinkedInService linkedInService, 
                HttpContext context) =>
            {
                var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Results.Unauthorized();
                }

                var skills = await linkedInService.GetSkillsAsync(userId);
                return skills != null ? Results.Ok(skills) : Results.NotFound();
            })
            .WithName("GetLinkedInSkills")
            .WithOpenApi();

            // Refresh LinkedIn token
            group.MapPost("/refresh-token", async (
                ILinkedInService linkedInService, 
                HttpContext context) =>
            {
                var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Results.Unauthorized();
                }

                var success = await linkedInService.RefreshLinkedInTokenAsync(userId);
                
                if (success)
                {
                    return Results.Ok(new { Message = "LinkedIn token refreshed successfully" });
                }
                else
                {
                    return Results.BadRequest("Failed to refresh LinkedIn token");
                }
            })
            .WithName("RefreshLinkedInToken")
            .WithOpenApi();
        }
    }
}