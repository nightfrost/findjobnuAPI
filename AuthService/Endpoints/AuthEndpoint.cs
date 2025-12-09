using AuthService.Models;
using Microsoft.AspNetCore.Http.HttpResults;
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
                var registerResult = await authService.RegisterAsync(request);
                if (!registerResult.Success)
                {
                    return Results.BadRequest(new { message = registerResult.ErrorMessage ?? "Registration failed." });
                }
                return Results.Ok(registerResult.AuthResponse);
            })
            .Produces<AuthResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .WithName("Register")
            .WithSummary("Registers a new user.")
            .WithDescription("Registers a new user with the provided email, password, and optional phone number.");

            authGroup.MapPost("/login", async (LoginRequest request, IAuthService authService) =>
            {
                var authResponse = await authService.LoginAsync(request);
                if (!authResponse.Success)
                {
                    return Results.BadRequest(new { message = authResponse.ErrorMessage ?? "Login failed." });
                }
                return Results.Ok(authResponse.AuthResponse);
            })
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
            .Produces(StatusCodes.Status302Found)
            .Produces(StatusCodes.Status400BadRequest)
            .WithName("ConfirmEmail")
            .WithSummary("Confirms a user's email address.")
            .WithDescription("Confirms the user's email using the provided userId and token, then redirects to https://findjob.nu on success.");

            authGroup.MapGet("/linkedin-verification/{userId}", async (string userId, IAuthService authService) =>
            {
                var isLinkedInUser = await authService.IsLinkedInUserOrHasVerifiedTheirLinkedIn(userId);
                if (isLinkedInUser.Item1)
                {
                    return Results.Ok(isLinkedInUser.Item2);
                }
                return Results.NoContent();
            })
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status204NoContent)
            .WithName("Verify LinkedIn connection.")
            .WithSummary("Confirms wether a user has verified through LinkedIn.")
            .WithDescription("Confirms the user's LinkedIn verification using the provided userId, then returns true or false.");

            authGroup.MapGet("/user-info", async Task<Results<Ok<UserInformationResult>, NotFound<UserInformationResult>>> (HttpContext context, IAuthService authService) =>
            {
                var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException("User ID not found in claims.");
                var userInfo = await authService.GetUserInformationAsync(userId);
                if (!userInfo.Success)
                    return TypedResults.NotFound(userInfo);
                return TypedResults.Ok(userInfo);
            })
            .RequireAuthorization()
            .Produces<UserInformationResult>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<UserInformationResult>(StatusCodes.Status404NotFound)
            .WithName("GetUserInformation")
            .WithSummary("Gets the user information for the current context user.")
            .WithDescription("Checks if the user has verified their LinkedIn account using the provided userId. Returns the LinkedInId on success, or NoContent if the user is not verified or not a LinkedIn user.");

            authGroup.MapPost("/change-password", async Task<Results<Ok<string>, UnauthorizedHttpResult, BadRequest<string>, ForbidHttpResult>> (ChangePasswordRequest request, HttpContext context, IAuthService authService) =>
            {
                var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return TypedResults.Unauthorized();

                if (string.IsNullOrEmpty(request.UserId))
                    return TypedResults.BadRequest("User ID in request is required.");

                if (string.IsNullOrEmpty(request.OldPassword) || string.IsNullOrEmpty(request.NewPassword))
                    return TypedResults.BadRequest("Current password and new password are required.");

                if (request.UserId != userId)
                    return TypedResults.Forbid();

                var changeResult = await authService.UpdatePasswordAsync(request.UserId, request.OldPassword, request.NewPassword);
                if (!changeResult.Succeeded)
                {
                    return TypedResults.BadRequest(changeResult.Errors?.FirstOrDefault()?.Description ?? "Failed to update password.");
                }
                // Optional: revoke all tokens after password change
                await authService.RevokeAllRefreshTokensAsync(userId);
                return TypedResults.Ok("Password updated successfully");
            })
            .RequireAuthorization()
            .Produces<string>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<string>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden)
            .WithName("ChangePassword")
            .WithSummary("Changes the user's password.")
            .WithDescription("Changes the user's password. Requires the current password and new password. Returns success message on success, or appropriate error messages on failure.");

            authGroup.MapPost("/lockout", async ([FromBody] string userId, HttpContext context, IAuthService authService) =>
            {
                var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim))
                    return Results.Unauthorized();

                if (userIdClaim != userId)
                    return Results.Forbid();

                if (string.IsNullOrEmpty(userId))
                    return Results.BadRequest(new { message = "User ID is required." });

                var result = await authService.LockoutUserAsync(userId);
                if (!result.Succeeded)
                {
                    var error = result.Errors?.FirstOrDefault()?.Description ?? "Failed to lockout user.";
                    return Results.BadRequest(new { message = error });
                }
                return Results.Ok(new { message = "User locked out successfully." });
            })
            .RequireAuthorization()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .WithName("LockoutUser")
            .WithSummary("Locks out a user account.")
            .WithDescription("Locks out the specified user by userId. Requires authorization.");

            // Change email (initiate)
            authGroup.MapPost("/change-email", async Task<Results<Ok<string>, UnauthorizedHttpResult, BadRequest<string>, ForbidHttpResult>> (ChangeEmailRequest request, HttpContext context, IAuthService authService) =>
            {
                var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return TypedResults.Unauthorized();

                if (request.UserId != userId)
                    return TypedResults.Forbid();

                var res = await authService.ChangeEmailAsync(request.UserId, request.NewEmail, request.CurrentPassword);
                if (!res.Succeeded)
                {
                    return TypedResults.BadRequest(res.Errors?.FirstOrDefault()?.Description ?? "Failed to initiate email change.");
                }
                return TypedResults.Ok("Email change confirmation sent.");
            })
            .RequireAuthorization()
            .Produces<string>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<string>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden)
            .WithName("ChangeEmail")
            .WithSummary("Initiates an email change.")
            .WithDescription("Sends a confirmation link to the new email. Requires current password.");

            // Confirm change email (via emailed link)
            authGroup.MapGet("/confirm-change-email", async ([FromQuery] string userId, [FromQuery] string newEmail, [FromQuery] string token, IAuthService authService) =>
            {
                var res = await authService.ConfirmChangeEmailAsync(userId, newEmail, token);
                if (!res.Succeeded)
                {
                    return Results.BadRequest(new { message = res.Errors?.FirstOrDefault()?.Description ?? "Failed to confirm email change." });
                }
                return Results.Redirect("https://findjob.nu");
            })
            .Produces(StatusCodes.Status302Found)
            .Produces(StatusCodes.Status400BadRequest)
            .WithName("ConfirmChangeEmail")
            .WithSummary("Confirms a user's email change.")
            .WithDescription("Confirms the user's email change using the provided token and updates username.");

            // Disable account (explicit)
            authGroup.MapPost("/disable-account", async Task<Results<Ok<string>, UnauthorizedHttpResult, BadRequest<string>, ForbidHttpResult>> (DisableAccountRequest request, HttpContext context, IAuthService authService) =>
            {
                var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return TypedResults.Unauthorized();
                if (request.UserId != userId)
                    return TypedResults.Forbid();

                var res = await authService.DisableAccountAsync(request.UserId, request.CurrentPassword);
                if (!res.Succeeded)
                {
                    return TypedResults.BadRequest(res.Errors?.FirstOrDefault()?.Description ?? "Failed to disable account.");
                }
                return TypedResults.Ok("Account disabled.");
            })
            .RequireAuthorization()
            .Produces<string>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<string>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden)
            .WithName("DisableAccount")
            .WithSummary("Disables the current user account.")
            .WithDescription("Locks out the user indefinitely and revokes all refresh tokens.");
        }
    }
}
