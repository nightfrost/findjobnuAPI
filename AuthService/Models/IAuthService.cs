using Microsoft.AspNetCore.Identity;

namespace AuthService.Models
{
    public interface IAuthService
    {
        Task<RegisterResult> RegisterAsync(RegisterRequest request, bool isLinkedInUser = false);
        Task<LoginResult> LoginAsync(LoginRequest request, bool isLinkedInUser = false);
        Task<AuthResponse?> RefreshTokenAsync(TokenRefreshRequest request);
        Task<bool> RevokeRefreshTokenAsync(string userId, string refreshToken);
        Task<bool> ConfirmEmailAsync(string userId, string token);
        Task<Tuple<bool, string?>> IsLinkedInUserOrHasVerifiedTheirLinkedIn(string userId);
        Task<UserInformationResult> GetUserInformationAsync(string userId);
        Task<IdentityResult> LockoutUserAsync(string userId);
        Task<IdentityResult> UpdatePasswordAsync(string userId, string oldPassword, string newPassword);
        Task<IdentityResult> ChangeEmailAsync(string userId, string newEmail, string currentPassword);
        Task<IdentityResult> ConfirmChangeEmailAsync(string userId, string newEmail, string token);
        Task<IdentityResult> DisableAccountAsync(string userId, string currentPassword);
        Task RevokeAllRefreshTokensAsync(string userId);
    }
}
