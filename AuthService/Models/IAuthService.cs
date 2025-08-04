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
    }
}
