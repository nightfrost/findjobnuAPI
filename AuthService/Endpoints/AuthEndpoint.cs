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
                               .WithTags("Authentication"); // Group endpoints in Swagger UI

            // Register Endpoint
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
            .WithDescription("Registers a new user with the provided email and password.");

            // Login Endpoint
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
        }
    }
}
