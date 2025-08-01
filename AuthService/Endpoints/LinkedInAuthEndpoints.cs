using AuthService.Services;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using AuthService.Models;

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
    }
}
