namespace AuthService.Models
{
    public interface IAuthService
    {
        Task<RegisterResult> RegisterAsync(RegisterRequest request);
        Task<AuthResponse?> LoginAsync(LoginRequest request);
        Task<AuthResponse?> RefreshTokenAsync(TokenRefreshRequest request);
        Task<bool> RevokeRefreshTokenAsync(string userId, string refreshToken);
        Task<bool> ConfirmEmailAsync(string userId, string token);
    }
}
