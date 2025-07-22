using AuthService.Models;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AuthService.Endpoints
{
    public static class AuthEndpoints
    {
        public static void MapAuthEndpoints(this WebApplication app)
        {
            var authGroup = app.MapGroup("/api/auth")
                               .WithTags("Authentication"); 

            authGroup.MapPost("/register", async (RegisterRequest request, IAuthService authService) =>
            {
                var authResponse = await authService.RegisterAsync(request);
                if (authResponse == null)
                {
                    return Results.BadRequest(new { message = "Registration failed. Email might already be taken or password requirements not met." });
                }
                return Results.Ok(authResponse);
            })
            .WithOpenApi()
            .Produces<AuthResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .WithName("Register")
            .WithSummary("Registers a new user.")
            .WithDescription("Registers a new user with the provided email, password, and optional phone number.");

            authGroup.MapPost("/login", async (LoginRequest request, IAuthService authService) =>
            {
                var authResponse = await authService.LoginAsync(request);
                if (authResponse == null)
                {
                    return Results.Unauthorized(); 
                }
                return Results.Ok(authResponse);
            })
            .WithOpenApi()
            .Produces<AuthResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .WithName("Login")
            .WithSummary("Logs in an existing user.")
            .WithDescription("Authenticates a user with email and password, returning JWT and Refresh Token.");

            
            authGroup.MapPost("/refresh-token", async (TokenRefreshRequest request, IAuthService authService) =>
            {
                var authResponse = await authService.RefreshTokenAsync(request);
                if (authResponse == null)
                {
                    return Results.Unauthorized(); 
                }
                return Results.Ok(authResponse);
            })
            .WithOpenApi()
            .Produces<AuthResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .WithName("RefreshToken")
            .WithSummary("Refreshes the access token.")
            .WithDescription("Exchanges an expired access token and a valid refresh token for new tokens.");

            authGroup.MapPost("/revoke-token", async (HttpContext context, [FromQuery] string refreshToken, IAuthService authService) =>
            {
                var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Results.Unauthorized();
                }

                var success = await authService.RevokeRefreshTokenAsync(userId, refreshToken);
                if (!success)
                {
                    return Results.BadRequest(new { message = "Failed to revoke token. Token might be invalid or not belong to user." });
                }

                return Results.Ok(new { message = "Token revoked successfully." });
            })
            .RequireAuthorization()
            .WithOpenApi()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status400BadRequest)
            .WithName("RevokeToken")
            .WithSummary("Revokes a specific refresh token.")
            .WithDescription("Invalidates a refresh token immediately. Requires a valid access token.");

            // Protected Data Endpoint (Example)
            authGroup.MapGet("/protected-data", (HttpContext context) =>
            {
                var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userEmail = context.User.FindFirst(ClaimTypes.Email)?.Value;
                return Results.Ok($"This is protected data. Only authenticated users can see this. Your ID: {userId}, Email: {userEmail}");
            })
            .RequireAuthorization() 
            .WithOpenApi()
            .Produces<string>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .WithName("GetProtectedData")
            .WithSummary("Retrieves protected data.")
            .WithDescription("An example endpoint that requires authentication to access.");

            authGroup.MapGet("/confirm-email", async ([FromQuery] string userId, [FromQuery] string token, IAuthService authService) =>
            {
                var success = await authService.ConfirmEmailAsync(userId, token);
                if (success)
                {
                    return Results.Redirect("https://findjob.nu");
                }
                return Results.BadRequest(new { message = "Email confirmation failed. Invalid token or user." });
            })
            .WithOpenApi()
            .Produces(StatusCodes.Status302Found)
            .Produces(StatusCodes.Status400BadRequest)
            .WithName("ConfirmEmail")
            .WithSummary("Confirms a user's email address.")
            .WithDescription("Confirms the user's email using the provided userId and token, then redirects to https://findjob.nu on success.");
        }
    }
}
